# MarketMaker.io

QFin UWA present exchangeMaker.io. This repository is for the backend server-side code for the .NET web app.

The server uses websockets through a SignalR wrapper to deliver realtime exchange making games. 

There is some basic front-end code in this repo for testing purposes.

## High-Level Overview

This application provides an API endpoint for websocket communication. The server processes each request 
asynchronously and will update the appropriate clients when there are updates to the exchange.

The user either joins or creates a exchange and they are placed in the lobby. They are an unnamed spectator until
they provide a username and join the exchange. After the admin opens the exchange they can place and delete orders.
Any special functionality (like ordering at the most competitive price) can be done by the client-side.

The client-side should use a ``signalR`` library to define callbacks for any functions the server is expecting. For 
example you could create a callback for the ``DeletedOrder`` function that removes the order from the user's screen. 
The client can similarly invoke functions on the server such as placing a new order or joining the game. The admin has
a special set of functions that only they are authorized to invoke.

Currently any exchanges will persist for 1 hour after the lobby is empty and will be deleted. The exchanges are serializable
so they can be saved by the admin as a JSON if desired. There are future plans to upload this to a database.

## Contracts

Contracts are records in .NET that standardize communication from the client to the server across invokable functions.

### Request Contracts

Request Contracts can be passed in from the client-side as a ``javascript`` object.

For example:
```js
var exchangeConfigRequest = {
    exchangeName: "Shoe exchange",
    MarketNames: {
        "A" : "Sneakers",
        "B" : "Laces",
        "C" : "Sandals",
    }
}

connection.invoke("UpdateConfig", exchangeConfigRequest);
```
If a field is excluded it will appear as ``null`` on the server. Note the value for ``MarketNames`` is a 
``<string, string>`` dictionary, not an object.

#### Config Update (Admin)

```csharp
public record ConfigUpdateRequest(
    string? exchangeName,
    Dictionary<string, string>? MarketNames
);
```

#### Delete Order (User)
```csharp
public record DeleteOrderRequest(
   Guid Id
);
```

#### New Order (User) 
```csharp
public record NewOrderRequest(
   string Market,
   int Price,
   int Quantity,
   string requestReference
);
```

### Response Contracts

These are contracts that are sent to the client. In ``javascript`` you can expect these to be objects. Similar 
to the request contracts, other functions not listed use a simple datatype like ``int`` or ``string``.

See example client-side code:

```javascript
connection.on("NewOrder", response => {
    console.log(`${response.Market}`)
    console.log(`${response.Price}`)
    console.log(`${response.Quantity}`)
    console.log(`${response.timeStamp}`)
});
```
#### DeleteOrder
```csharp
public record DeleteOrderResponse(
   Guid Id
);
```

#### LobbyState 
```csharp
public record LobbyStateResponse(
    List<List<string?>> Markets,
    List<string> Participants,
    string State,
    string ExchangeName,
    string ExchangeCode
);
```
#### ExchangeState
```csharp
public record exchangeStateResponse(
    List<Order> Orders,
    List<Transaction> Transactions
);
```
#### OrderReceived

When an order is placed by a client, the server will response to only that user with a ``OrderReceivedResponse`` that 
effectively informs them which order corresponds to their request, of which the user attaches a custom label ``RequestReference``.

```csharp
public record OrderReceivedResponse(
    Guid CreatedOrder, 
    string RequestReference
);
```

#### NewOrder 

The market should always be in a balanced state. If a user places an order the server will respond to all users 
with the new order to be placed in the order-book. If the order traded with any existing orders the quantity will be 
affected and the list of transactions will be non-zero length.

```csharp
public record NewOrderResponse(
   string User,
   string Market,
   int Price,
   int Quantity,
   DateTime TimeStamp,
   Guid Id
   List<Transaction> Transactions
);

// Transaction Definition
public record TransactionResponse(
    string BuyerUser,
    Guid BuyerOrderId,
    string SellerUser,
    Guid SellerOrderId,
    string market,
    int Price,
    int Quantity,
    string Aggressor,
    DateTime TimeStamp
);
```
## Server Methods

These are methods that can be invoked by the client by using ``connection.on("[method name]", [args])``

### Admin Methods

```csharp
// Creates a new Lobby
// Invokes: LobbyState
public async Task MakeNewexchange()
 
// Creates a new exchange from a JSON serialized string 
// Invokes: LobbyState, exchangeState
public async Task Loadexchange(string jsonSerialized)
 
// Creates a new market on the exchange
// Invokes: LobbyState
public async Task MakeNewMarket()

// Updates the config of the exchange 
// Invokes: LobbyState
public async Task UpdateConfig(ConfigUpdateRequest configUpdate)

// Updates the state of the exchange 
// Invokes: StateUpdated
public async Task UpdateExchangeState(string newStateString)

// Closes the exchange at an optional price
// Invokes: StateUpdated and/or ClosingPrice
public async Task CloseExchange(Dictionary<string, int>? closePrices= null)

// Serializes the exchange into a JSON string
// Invokes: ReceiveMessage
public async Task Serialize()
```

### User Methods
```csharp
// Joins the exchange as a participant
// Invokes: NewParticipant
public async Task Joinexchange(string username) 
    
// Places an order
// Invokes: NewOrder
public async Task PlaceOrder(string market, int price, int quantity, string requestReference)

// Deletes an order
// Invokes: DeletedOrder 
public async Task DeleteOrder(Guid orderId)
```
## States

The exchange can be in any of the four states listed below. 
```
           [Paused]
             ^      \
             |       V
[Lobby] -> [Open] -> [Closed]
    ^__________________/
```


### Lobby
Represents the exchange as it is being configured and before the game starts. The exchange can only be configured in this state.

**Allowed Transitions:** Open

### Open
Represents the exchange being open for trading. This is the only state where trades can be made.

**Allowed Transitions:** Paused, Closed 

### Paused
Represents the exchange being temporarily paused from trading by the admin.

**Allowed Transitions:** Open, Closed 

### Closed
Represents the exchange when trading has completed, for example when the exchange closes at a price.

**Allowed Transitions:** Lobby 


# API 

Authentication and authorization is implemented using JWT tokens.

To obtain an auth token, use the POST HTTP endpoint ``/login`` with the body: ``{Email: [email], Password: [password]}"``.
Note, you may have to strip the ``"`` character from the return string.

To create an exchange, use the GET HTTP endpoint ``/createExchange``. You must provide an ``Authorization`` header with the value ``Bearer [JWT Token]``, 
where the ``JWT Token`` is provided by ``/login``.

To join an exchange, use the GET HTTP endpoint ``/joinExchange?exchangeCode=[exchangeCode]``. The ``Authorization`` header is optional. By providing it, the user will have access to all markets they
have accessed in the past (as their ``userID`` is stored in each exchange). 

After hitting the creation/join HTTP endpoints, a new ``JWT Token`` will be returned that authorizes access to the 
signalr hub with the issued credentials (eg, Is the user an admin? What is their userID? What exchange are they accessing?).

In ``javascript`` the connection can then be established as follows:

```js
const connection = new signalR.HubConnectionBuilder()
.withUrl(
  serverURL + "/exchange", {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets,
accessTokenFactory: () => jwtToken,
})
.build();

connection.start();
```
