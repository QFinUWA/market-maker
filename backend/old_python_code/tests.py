import pytest
from app import app, db, User, OrderBook, Order
from flask import json

@pytest.fixture
def client():
    app.config['TESTING'] = True
    app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///:memory:'
    client = app.test_client()

    with app.app_context():
        db.create_all()

    yield client

    with app.app_context():
        db.session.remove()
        db.drop_all()

def test_user_creation(client):
    rv = client.post('/users', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    assert b'New user created.' in rv.data

def test_duplicate_user_creation(client):
    rv = client.post('/users', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    assert b'New user created.' in rv.data

    rv = client.post('/users', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    assert b'Username already exists.' in rv.data

def test_orderbook_creation(client):
    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    assert b'New orderbook created.' in rv.data

def test_duplicate_orderbook_creation(client):
    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    assert b'New orderbook created.' in rv.data

    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    assert b'Orderbook already exists.' in rv.data

def test_order_creation(client):
    # Create a user and an orderbook before creating an order
    rv = client.post('/users', data=json.dumps(dict(
        username='test_user_1',
        password='password'
    )), content_type='application/json')

    user = rv.get_json()['user']

    # Login using /login GET
    rv = client.post('/login', data=json.dumps(dict(
        username='test_user_1',
        password='password'
    )), content_type='application/json')

    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    orderbook = rv.get_json()['orderbook']

    rv = client.post('/orders', data=json.dumps(dict(
        orderbook_id=orderbook['id'],
        user_id=user['id'],
        side='buy',
        price=100,
        quantity=10
    )), content_type='application/json')

    assert b'Order created.' in rv.data

    # Get the orderbook and verify the created order
    rv = client.get(f'/orderbook/{orderbook["id"]}')
    data = json.loads(rv.data)
    assert len(data['orders']) == 1
    assert data['orders'][0]['user_id'] == user['id']
    assert data['orders'][0]['price'] == 100
    assert data['orders'][0]['quantity'] == 10


def test_order_cancellation(client):
    # Create a user and an orderbook before creating an order
    rv = client.post('/users', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    user = rv.get_json()['user']

    # Login
    rv = client.post('/login', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    orderbook = rv.get_json()['orderbook']

    rv = client.post('/orders', data=json.dumps(dict(
        orderbook_id=orderbook['id'],
        user_id=user['id'],
        side='buy',
        price=100,
        quantity=10
    )), content_type='application/json')


    order = rv.get_json()
    print(f'Order: {order}')
    order = order['order']

    rv = client.delete(f'/orders/{order["id"]}')

    print(f'rv.data: {rv.data}')

    assert b'Order cancelled.' in rv.data

    # Get the orderbook and verify the order has been removed
    rv = client.get(f'/orderbook/{orderbook["id"]}')
    data = json.loads(rv.data)
    print(f'data: {data}')
    database_order = list(filter(lambda x: x['id'] == order['id'], data['orders']))
    assert database_order[0]['status'] == 'CANCELLED'

def test_multiple_order_creation_and_cancellation(client):
    # Create a user and an orderbook before creating an order
    rv = client.post('/users', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    user = rv.get_json()['user']

    # Login
    rv = client.post('/login', data=json.dumps(dict(
        username='test_user',
        password='password'
    )), content_type='application/json')

    rv = client.post('/orderbook', data=json.dumps(dict(
        name='test_orderbook',
        instrument='test_instrument'
    )), content_type='application/json')

    orderbook = rv.get_json()['orderbook']

    # Create first order
    rv = client.post('/orders', data=json.dumps(dict(
        orderbook_id=orderbook['id'],
        user_id=user['id'],
        side='buy',
        price=100,
        quantity=10
    )), content_type='application/json')

    order1 = rv.get_json()['order']

    # Create second order
    rv = client.post('/orders', data=json.dumps(dict(
        orderbook_id=orderbook['id'],
        user_id=user['id'],
        side='sell',
        price=120,
        quantity=8
    )), content_type='application/json')

    order2 = rv.get_json()['order']

    # Cancel the first order
    rv = client.delete(f'/orders/{order1["id"]}')

    assert b'Order cancelled.' in rv.data

    # Get the orderbook and verify the first order has been cancelled
    rv = client.get(f'/orderbook/{orderbook["id"]}')
    data = json.loads(rv.data)
    database_order1 = list(filter(lambda x: x['id'] == order1['id'], data['orders']))
    assert database_order1[0]['status'] == 'CANCELLED'

    # Verify the second order is still active
    database_order2 = list(filter(lambda x: x['id'] == order2['id'], data['orders']))
    assert database_order2[0]['status'] != 'CANCELLED'