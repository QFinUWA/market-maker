# MarketMaker.io

QFin UWA present MarketMaker.io. This repository is for the backend server-side code for the .NET web app.

The server uses websockets through a SignalR wrapper to deliver realtime market making games. 

There is some basic front-end code in this repo for testing purposes.

## High-Level Overview

This application provides an API endpoint for websocket communication. The server processes each request 
asynchronously and will update the appropriate clients when there are updates to the market.

The user either joins or creates a market and they are placed in the lobby. They are an unnamed spectator until
they provide a username and join the market. After the admin opens the market they can place and delete orders.
Any special functionality (like ordering at the most competitive price) can be done by the client-side.

The client-side should use a ``signalR`` library to define callbacks for any functions the server is expecting. For 
example you could create a callback for the ``DeletedOrder`` function that removes the order from the user's screen. 
The client can similarly invoke functions on the server such as placing a new order or joining the game. The admin has
a special set of functions that only they are authorized to invoke.

Currently any markets will persist for 1 hour after the lobby is empty and will be deleted. The markets are serializable
so they can be saved by the admin as a JSON if desired. There are future plans to upload this to a database.

## Contracts

Contracts are records in .NET that standardize communication from the client to the server across invokable functions.

### Request Contracts

Request Contracts can be passed in from the client-side as a ``javascript`` object.

For example:
```js
var marketConfigRequest = {
    MarketName: "Shoe Market",
    ExchangeNames: {
        "A" : "Sneakers",
        "B" : "Laces",
        "C" : "Sandals",
    }
}

connection.invoke("UpdateConfig", marketConfigRequest);
```
If a field is excluded it will appear as ``null`` on the server. Note the value for ``ExchangeNames`` is a 
``<string, string>`` dictionary, not an object.

#### Config Update (Admin)

```csharp
public record ConfigUpdateRequest(
    string? MarketName,
    Dictionary<string, string>? ExchangeNames
);
```

#### Delete Order (User)
```csharp
public record DeleteOrderRequest(
   string Exchange,
   Guid Id
);
```

#### New Order (user) 
```csharp
public record NewOrderRequest(
   string Exchange,
   int Price,
   int Quantity
);
```

### Response Contracts

These are contracts that are sent to the client-side. In ``javascript`` you can expect these to be objects. Similar 
to the request contracts, other functions not listed use simple datatypes like ``int`` or ``string``.

See example client-side code:

```javascript
connection.on("NewTransaction", response => {
    console.log(`${response.buyerUser} bought from ${response.sellerUser}`)
    console.log(`${response.quantity} units of ${response.quantity}  for ${response.price}`)
    console.log(`${response.timeStamp}`)
});
```
#### Delete Order
```csharp
public record DeleteOrderResponse(
   Guid Id
);
```

#### Lobby State 
```csharp
public record LobbyStateResponse(
    List<List<string?>> Exchanges,
    List<string> Participants,
    string State,
    string MarketName,
    string MarketCode
);
```
#### Market State
```csharp
public record MarketStateResponse(
    List<Order> Orders,
    List<Transaction> Transactions
);
```
#### New Order 
```csharp
public record NewOrderResponse(
   string User,
   string Exchange,
   int Price,
   int Quantity,
   DateTime TimeStamp,
   Guid Id
);
```
#### Transaction
```csharp
public record TransactionResponse(
    string BuyerUser,
    Guid BuyerOrderId,
    string SellerUser,
    Guid SellerOrderId,
    string exchange,
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
public async Task MakeNewMarket()
 
// Creates a new market from a JSON serialized string 
// Invokes: LobbyState, MarketState
public async Task LoadMarket(string jsonSerialized)
 
// Creates a new exchange on the market
// Invokes: LobbyState
public async Task MakeNewExchange()

// Updates the config of the market 
// Invokes: LobbyState
public async Task UpdateConfig(ConfigUpdateRequest configUpdate)

// Updates the state of the market 
// Invokes: StateUpdated
public async Task UpdateMarketState(string newStateString)

// Closes the market at an optional price
// Invokes: StateUpdated and/or ClosingPrice
public async Task CloseMarket(Dictionary<string, int>? closePrices= null)

// Serializes the market into a JSON string
// Invokes: ReceiveMessage
public async Task Serialize()
```

### User Methods
```csharp
// Joins a new lobby as a spectator
// Invokes: LobbyState, MarketState
public async Task JoinMarketLobby(string groupName)

// Joins the market as a participant
// Invokes: NewParticipant
public async Task JoinMarket(string username) 
    
// Places an order
// Invokes: NewOrder and TransactionEvent
public async Task PlaceOrder(string exchange, int price, int quantity)

// Deletes an order
// Invokes: DeletedOrder 
public async Task DeleteOrder(Guid orderId)
```
## States

```
           [Paused]
             ^      \
             |       V
[Lobby] -> [Open] -> [Closed]
    ^__________________/
```


### Lobby
Represents the market as it is being configured and before the game starts.
#### Allowed Transitions
Open

### Open
Represents the market being open for trading.
#### Allowed Transitions
Paused, Closed

### Paused
Represents the market being temporarily paused from trading by the admin.
#### Allowed Transitions
Open, Closed

### Closed
Represents the market when trading has completed, for example when the market closes at a price.
#### Allowed Transitions
Lobby

## Future Direction
- Add OAuth for secure sign-in
- Add a database for storing users and previous games
- Scale the application for 1000s of users and add a rate limiter. 