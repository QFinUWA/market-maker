from app import db
from datetime import datetime
from werkzeug.security import generate_password_hash, check_password_hash

class User(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(64), unique=True, index=True)
    password_hash = db.Column(db.String(128))

    def set_password(self, password):
        self.password_hash = generate_password_hash(password)

    def check_password(self, password):
        return check_password_hash(self.password_hash, password)


class OrderBook(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), unique=True, nullable=False)
    instrument = db.Column(db.String(50), unique=True, nullable=False)
    timestamp = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    orders = db.relationship('Order', backref='orderbook', lazy='dynamic')

class Order(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    order_book_id = db.Column(db.Integer, db.ForeignKey('orderbook.id'))
    user_id = db.Column(db.Integer, db.ForeignKey('user.id'))
    price = db.Column(db.Float)
    quantity = db.Column(db.Float)
    order_type = db.Column(db.String(4))  # "BUY" or "SELL"
    fill_quantity = db.Column(db.Float, default=0)
    status = db.Column(db.String(20), default='OPEN') # "OPEN", "FILLED", "CANCELLED"
    timestamp = db.Column(db.DateTime, default=datetime.utcnow)