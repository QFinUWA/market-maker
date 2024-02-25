# Notes
## Account
### Logging in
Logging into an account is a POST request to `${serverURL}/login` with the following body:
```json
JSON.stringify({
    "Email": string,
    "Password": string
});
```
The header should contain the following:
```json
{
    "Accept": "application/json",
    "Content-Type": "application/json"
}
```
If the response is ok, the response.Text() will contain the user's token.
token = token.replace(/"/g, "");

### Creating an account
Creating an account is a POST request to `${serverURL}/createAccount` with the following body:
```json
JSON.stringify({
    "Email": string,
    "Password": string
});
```
The header should contain the following:
```json
{
    "Accept": "application/json",
    "Content-Type": "application/json"
}
```
If the response is ok, then the account was created successfully. An account cannot be created with an email that already exists.

## Exchange
There are two ways to join an exchange. The first is by creating an exchange and the second is by joining an exchange. Both actions return a JWT token that is used to authenticate the user.

### Creating an exchange
Creating an exchange requires the user to be logged in.

### Joining an exchange
Joining an exchange is an anonymous action.

##




`${serverURL}/joinExchange?exchangeCode=${exchangeCode}`

### What can a user do once they are in an exchange?
- View the exchange's information
- View the exchange's members
- View the exchange's transactions
- Place a transaction
- View their own transactions
- View their own balance
- View their own transaction history

The priority is:
1. View the exchange's information, ability to place a transaction and view the transaction history

## Orders

Keep a list of all orders. 
Have a separate list of all transactions. 
Transactions create a new order with quantity 0 if there is none left over. 
Figuring out which orders to show can be calculated based on the orders and transactions list. 
Ideally want to keep as much information as possible (not deleting out of orders list if it was filled) for reporting purposes at the end. 

## Thoughts
On join, establish signalR connection to the exchange using the JWT, or redirect to the login page if the JWT is invalid.
Load the exchange's information, members, and transactions.
Display the exchange's information, members, and transactions.
Allow the user to place a transaction and view their transaction history.