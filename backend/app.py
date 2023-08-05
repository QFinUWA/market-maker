from models import User, Order, OrderBook
from flask import Flask, request, jsonify
from flask_sqlalchemy import SQLAlchemy
from flask_login import current_user, login_required, login_user
from flask_sse import sse
import redis
from database import db

app = Flask(__name__)
app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///site.db'
app.config["REDIS_URL"] = "redis://localhost"
app.register_blueprint(sse, url_prefix='/stream')
db.init_app(app)


@app.route('/users', methods=['POST'])
def create_user():
    data = request.get_json()
    username = data['username']
    password = data['password']

    new_user = User.create(username, password)

    if new_user is None:
        return jsonify({'message': 'Username already exists.'}), 400

    return jsonify({'message': 'New user created.'}), 201


@app.route('/login', methods=['POST'])
def login():
    data = request.get_json()
    username = data['username']
    password = data['password']

    user = User.query.filter_by(username=username).first()

    if user and user.check_password(password):
        login_user(user)
        return jsonify({'message': 'Logged in successfully.'}), 200

    return jsonify({'message': 'Invalid username or password.'}), 401


@app.route('/orders', methods=['POST'])
@login_required
def create_order():
    data = request.get_json()
    order_book_id = data['orderbook_id']
    user_id = data['user_id']
    side = data['side']
    price = data['price']
    quantity = data['quantity']

    order_book = OrderBook.query.get(order_book_id)

    if not order_book:
        return jsonify({'message': 'Order book not found.'}), 404

    user = User.query.get(user_id)

    if not user:
        return jsonify({'message': 'User not found.'}), 404

    if current_user.id != user_id:
        return jsonify({'message': 'You do not have permission to create this order.'}), 403

    order = Order.create_and_fill(
        order_book_id, user_id, side, price, quantity)

    return jsonify({'message': 'Order created.', 'order': order.to_dict()}), 201


@app.route('/orders/<order_id>', methods=['DELETE'])
@login_required
def cancel_order(order_id):
    order = Order.query.get(order_id)

    if not order:
        return jsonify({'message': 'No order found.'}), 404

    if current_user.id != order.user_id:
        return jsonify({'message': 'You do not have permission to cancel this order.'}), 403

    try:
        order.cancel()
    except ValueError as e:
        return jsonify({'message': str(e)}), 400

    return jsonify({'message': 'Order cancelled.'}), 200


@app.route('/orders/<order_id>/trade', methods=['POST'])
@login_required
def trade_order(order_id):
    data = request.get_json()
    traded_quantity = data.get('quantity')

    try:
        new_order, unfulfilled_quantity = Order.trade(
            order_id, current_user.id, traded_quantity)
    except ValueError as e:
        return jsonify({'message': str(e)}), 400

    return jsonify({
        'new_order': new_order.to_dict(),
        'unfulfilled_quantity': unfulfilled_quantity
    }), 200


@app.route('/orderbook/<order_book_id>', methods=['GET'])
def view_order_book(order_book_id):
    order_book = OrderBook.query.get(order_book_id)
    if order_book is None:
        return jsonify({'message': 'Order book not found.'}), 404

    return jsonify(order_book.to_dict()), 200

@app.route('/orderbook', methods=['POST'])
def create_orderbook():
    data = request.get_json()
    name = data['name']
    instrument = data['instrument']

    new_orderbook = OrderBook.create(name, instrument)

    if new_orderbook is None:
        return jsonify({'message': 'Orderbook already exists.'}), 400

    return jsonify({'message': 'New orderbook created.'}), 201


if __name__ == "__main__":
    app.run(debug=True)
