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
