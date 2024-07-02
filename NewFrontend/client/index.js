import { Exchange } from "./Exchange/exchange";
import { Player } from "./player";
import { Navigator } from "./navigation";

let positionLimit = 1000; // temp variable, will be provided by server after backend refactoring

// const serverURL = "https://market-maker.azurewebsites.net/";

// updates all the content on the page
function updateContent() {
    navigator.updateOrderTables();
    navigator.updateProductViews();
    navigator.updateTransactionTables();
}

function updateModal() {
    document.getElementById("exchangeState").textContent = exchange.getState();
    const overlayMessage = document.getElementById("overlayMessage");
    if (exchange.state == "Lobby") {
        document.getElementById("overlay").classList.remove("hidden");
        overlayMessage.textContent = "Waiting for other players to join...";
    } else if (exchange.state == "Paused") {
        document.getElementById("overlay").classList.remove("hidden");
        overlayMessage.textContent = "Exchange Paused";
    } else {
        document.getElementById("overlay").classList.add("hidden");
    }
}


// generates the tabs for each market (should run every time markets change)
function initUI(tabNames, player, connection, exchange) {
    navigator.generateTabs(tabNames);
    navigator.initOrderTables();
    navigator.initOrderInputs();
    navigator.initProductViews();
    navigator.initTransactionTables();
    updateContent();
}


function bindConnection(jwt) {
    // Thank you Isaac
    // Called when creating a exchange or joining an exchange
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(serverURL + "exchange", {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets,
            accessTokenFactory: () => jwt,
        })
        // .configureLogging(signalR.LogLevel.Debug)
        .build();

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            // setTimeout(start, 50000000);
        }
    }

    connection.onclose(async () => {
        await start();
    });

    connection.on("ReceiveMessage", (message) => {
        console.log("server: " + message);
    });

    // Redraws the entire exchange
    // exchangeResponse should also include information about the player like position, open position, cash and realisedPnL (currently this manually calculates this info from the orders and transactions)
    connection.on("ExchangeState", (exchangeResponse) => {

        let orders = {};                                                                                            // To be removed upon better arguments
        let transactions = {};                                                                                      // To be removed upon better arguments
        Object.keys(exchange.getMarkets()).forEach((market) => {orders[market] = []; transactions[market] = [];});  // To be removed upon better arguments
        newMarkets.orders.forEach((order) => {                                                                      // To be removed upon better arguments
            orders[order.market].push(order);
        });
        newMarkets.transactions.forEach((transaction) => {                                                          // To be removed upon better arguments
            transactions[transaction.market].push(transaction);
        });

        let _realised_pnl = 0;                                                                                      // To be removed upon better arguments
        let _player_markets = {};                                                                                   // To be removed upon better arguments
        Object.keys(orders).forEach((market) => {                                                                   // To be removed upon better arguments
            _buys = 0;
            _sells = 0;
            _cash = 0;
            transactions[market].forEach((transaction) => {
                if (transaction.buyerUser == player.getName()){
                    _buys += transaction.quantity;
                    _cash -= transaction.price * transaction.quantity;
                } if (transaction.sellerUser == player.getName()){
                    _sells += transaction.quantity;
                    _cash += transaction.price * transaction.quantity;
                }
            });

            _open_bids = 0;
            _open_asks = 0;
            orders[market].forEach((order) => {
                if (order.user == player.getName()){
                    if (order.quantity > 0){
                        _open_bids += order.quantity;
                    } else {
                        _open_asks -= order.quantity;
                    }
                }
            });

            if(exchange.isClosed(market)){
                _cash -= (_buys - _sells) * closingPrice;
                _buys = Math.max(_buys, _sells);
                _sells = Math.max(_buys, _sells);
                _open_bids = 0;
                _open_asks = 0;
                _realised_pnl += _cash;
            }

            _player_markets[market] = {
                limit: positionLimit,
                buys: _buys,
                sells: _sells,
                open_bids: _open_bids,
                open_asks: _open_asks,
                cash: _cash,
            }
        });

        let _player = {                                                                                             // To be removed upon better arguments
            player_markets: _player_markets,
            realised_pnl: _realised_pnl,
        }
        
        exchange.setMarkets(orders, transactions);
        player.init(_player);

        console.log("ExchangeState", exchangeResponse, exchange);
        
        updateContent();
    });

    connection.on("StateUpdated", (newState) => {
        // exchange.state = newState;

        exchange.setState(newState);

        console.log("StateUpdated", exchange);
        updateModal()
    });

    // Called when client or another player joins lobby (I think) (Although it should not really be called when anotehr player joins)
    // Message should also include position limits for each market and whether each market is settled or unsettled
    connection.on("LobbyState", (message) => {

        exchange.init(message.markets, message.participants, message.exchangeName, message.exchangeCode, message.state);

        initUI(exchange.getMarkets());

        console.log("LobbyState", message, exchange);
    });

    // Currently the transactions list needs to be separated from the order object, it should look cleaner once the argument is already passed as something that separates the two
    connection.on("NewOrder", (order) => {

        let transactions = [...order.transactions]; // To be removed upon better arguments
        delete order.transactions;                  // To be removed upon better arguments
        market = order.market;

        exchange.newOrder(market, order, transactions);

        console.log("NewOrder", order, exchange);
        updateContent();
    });

    // Currently only orderID is being sent so the marketID needs to be found manually, it should look cleaner once the marketID is also passed as an argument
    connection.on("DeletedOrder", (orderID) => {

        let market = null;                                          // To be removed upon better arguments
        Object.keys(exchange.getMarkets()).forEach((_market) => {   // To be removed upon better arguments
            if(exchange.getMarketOrders(_market).some(order => order.id == orderID)){   
                market = _market;
            }
        });
        exchange.deleteOrder(market, orderID);

        console.log("DeletedOrder", orderID, exchange);
        updateContent();
    });

    connection.on("NewParticipant", (user) => {
        // exchange.participants.push(user);

        exchange.newParticipant(user);

        console.log("NewParticipant", user, exchange);
    });

    connection.on("OrderReceived", (orderList) => { // TODO it seems like this route never runs
        console.log("OrderReceived", orderList);

    });

    connection.on("ClosingPrices", (closingPrices) => {

        player.closeMarkets(closingPrices);
        exchange.closeMarkets(Object.keys(closingPrices));

        console.log("ClosingPrices", closingPrices);
    });

    return [connection, start];
}

// joinExchange needs to be run once on load to set your name and join
// just a button to join 
function joinExchange(name) {
    connection.invoke("JoinExchange", name).catch((err) => {
        console.error(err.toString());
    });
}

console.log(document.cookie);
// On load, checks if a jwt token is present in cookies
// If not, redirect to login page
if (document.cookie.indexOf("jwt") == -1) {
    window.location.href = "login.html";
}

const jwt = document.cookie
    .split(";")
    .find((c) => c.trim().startsWith("jwt="))
    .split("=")[1];
const name = document.cookie
    .split(";")
    .find((c) => c.trim().startsWith("name="))
    .split("=")[1];

document.getElementById("playerName").textContent = name;

var player = new Player(name);
var exchange = new Exchange();
document.getElementById("exchangeName").textContent = exchange.getName();
document.getElementById("exchangeCode").textContent = exchange.getCode();
document.getElementById("exchangeState").textContent = exchange.getState();
var navigator = new Navigator(document.getElementById('tab-navigation'), document.getElementById('tab-content'));

// Connect to the SignalR hub
const [connection, start] = bindConnection(jwt);
start()