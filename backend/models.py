from flask_login import current_user
from database import db
from datetime import datetime
from werkzeug.security import generate_password_hash, check_password_hash
from flask_sse import sse


class User(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(64), unique=True, index=True)
    password_hash = db.Column(db.String(128))

    def set_password(self, password):
        self.password_hash = generate_password_hash(password)

    def check_password(self, password):
        return check_password_hash(self.password_hash, password)

    @classmethod
    def create(cls, username, password):
        user = cls.query.filter_by(username=username).first()
        if user:
            return None

        new_user = cls(username=username)
        new_user.set_password(password)

        db.session.add(new_user)
        db.session.commit()
        return new_user

    def get_exposure(self, orderbook_id):
        buy_orders = Order.query.filter_by(
            user_id=self.id, orderbook_id=orderbook_id, side="BUY", status="OPEN").all()
        sell_orders = Order.query.filter_by(
            user_id=self.id, orderbook_id=orderbook_id, side="SELL", status="OPEN").all()

        buy_exposure = sum(
            [order.quantity - order.fill_quantity for order in buy_orders])
        sell_exposure = sum(
            [order.quantity - order.fill_quantity for order in sell_orders])

        return buy_exposure, sell_exposure


class OrderBook(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), unique=True, nullable=False)
    instrument = db.Column(db.String(50), unique=True, nullable=False)
    timestamp = db.Column(
        db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    orders = db.relationship('Order', backref='orderbook', lazy='dynamic')

    @classmethod
    def create(cls, name, instrument):
        orderbook = cls.query.filter_by(name=name).first()
        if orderbook:
            return None

        new_orderbook = cls(name=name, instrument=instrument)
        db.session.add(new_orderbook)
        db.session.commit()
        return new_orderbook

    def publish_order_book(order_book_id):
        # Query the database for the current state of the order book
        order_book = Order.query.filter_by(orderbook_id=order_book_id).all()
        order_book_json = [order.to_dict() for order in order_book]

        # Publish it to the stream
        sse.publish({"message": "Order book updated",
                    "order_book": order_book_json}, type='order_book_update')

    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'instrument': self.instrument,
            'timestamp': self.timestamp.isoformat(),
            'orders': [order.to_dict() for order in self.orders.all()]
        }


class Order(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    order_book_id = db.Column(db.Integer, db.ForeignKey('orderbook.id'))
    user_id = db.Column(db.Integer, db.ForeignKey('user.id'))
    price = db.Column(db.Float)
    quantity = db.Column(db.Float)
    order_type = db.Column(db.String(4))  # "BUY" or "SELL"
    fill_quantity = db.Column(db.Float, default=0)
    # "OPEN", "FILLED", "CANCELLED"
    status = db.Column(db.String(20), default="OPEN")
    timestamp = db.Column(db.DateTime, default=datetime.utcnow)

    def to_dict(self):
        return {
            'id': self.id,
            'user_id': self.user_id,
            'orderbook_id': self.orderbook_id,
            'side': self.side,
            'price': self.price,
            'quantity': self.quantity,
            'status': self.status,
            'fill_quantity': self.fill_quantity,
            'timestamp': self.timestamp.isoformat()  # convert datetime object to string
        }

    @classmethod
    def create_and_fill(cls, order_book_id, user_id, side, price, quantity):
        # Check if there are opposite side orders that can be filled
        opposite_orders = cls.query.filter_by(
            order_book_id=order_book_id, side="SELL" if side == "BUY" else "BUY", status="OPEN")

        # Sorting the opposite orders by best price and then oldest
        if side == "BUY":
            opposite_orders = opposite_orders.order_by(
                cls.price.desc(), cls.timestamp.asc()).all()
        else:  # side == "SELL"
            opposite_orders = opposite_orders.order_by(
                cls.price.asc(), cls.timestamp.asc()).all()

        for opposite_order in opposite_orders:
            if (side == "BUY" and price >= opposite_order.price) or (side == "SELL" and price <= opposite_order.price):
                fill_quantity = min(
                    quantity, opposite_order.quantity - opposite_order.fill_quantity)
                # Update the original order
                opposite_order.fill_quantity += fill_quantity
                opposite_order.status = "FILLED" if opposite_order.fill_quantity == opposite_order.quantity else "OPEN"
                # Create a new inverse order
                filled_order = cls(order_book_id=order_book_id, user_id=user_id, side=opposite_order.side,
                                   price=price, quantity=fill_quantity, fill_quantity=fill_quantity, status="FILLED")
                db.session.add(filled_order)
                # Decrease the quantity
                quantity -= fill_quantity
                if quantity == 0:
                    break

        # If there is still quantity left, create a new open order
        if quantity > 0:
            new_order = cls(order_book_id=order_book_id, user_id=user_id, side=side,
                            price=price, quantity=quantity, fill_quantity=0, status="OPEN")
            db.session.add(new_order)

        db.session.commit()

        return new_order if quantity > 0 else filled_order

    def cancel(self):
        if self.status != "OPEN":
            raise ValueError('Only open orders can be cancelled.')

        self.status = "CANCELLED"
        db.session.commit()

    @classmethod
    def trade(cls, order_id, user_id, quantity):
        existing_order = cls.query.get(order_id)
        
        if not existing_order:
            raise ValueError('No order found.')

        if existing_order.status != 'OPEN':
            raise ValueError('This order is not open.')

        remaining_quantity = existing_order.quantity - existing_order.fill_quantity

        fill_quantity = min(quantity, remaining_quantity)

        existing_order.fill_quantity += fill_quantity
        existing_order.status = 'FILLED' if existing_order.fill_quantity == existing_order.quantity else 'OPEN'

        new_order = cls(
            order_book_id=existing_order.order_book_id,
            user_id=user_id,
            side='SELL' if existing_order.side == 'BUY' else 'BUY',
            price=existing_order.price,
            quantity=fill_quantity,
            fill_quantity=fill_quantity,
            status='FILLED'
        )
        db.session.add(new_order)

        db.session.commit()

        unfulfilled_quantity = max(0, quantity - fill_quantity)

        return new_order, unfulfilled_quantity