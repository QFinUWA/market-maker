// get the element with id "market" and insert a list of items
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7221/market", {
  // .withUrl("https://market-maker-prod.azurewebsites.net/market", {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets,
  })
  .configureLogging(signalR.LogLevel.Debug)
  .build();

async function start() {
  try {
    await connection.start();
    console.log("SignalR Connected.");
  } catch (err) {
    console.log(err);
    setTimeout(start, 5000);
  }
}
var orders = [];
var exchanges = [];
var marketName = "";
// define function

function refreshMarket() {
  var ordersList = "<ul>";
  for (var i = 0; i < orders.length; i++) {
    ordersList += "<li>" + formatOrder(orders[i]) + "</li>";
  }
  ordersList += "</ul>";
  document.getElementById("marketName").innerHTML = "Market Code: " + marketName;
  document.getElementById("exchangeNames").innerHTML =
    "Exchanges: " + exchanges.join(", ");
  document.getElementById("market").innerHTML = ordersList;
}

function formatOrder(order) {
  console.log(order);
  return (
    order["exchange"] + 
    ") " + 
    order["timeStamp"] +
    ": " +
    " $" +
    order["price"] +
    ", quantity " +
    order["quantity"] +
    " <= " +
    order["user"]
  );
}

connection.onclose(async () => {
  await start();
});

connection.on("ReceiveMessage", (message) => {
  console.log("server: " + message);
});

connection.on("MarketState", (market) => {
  // console.log(market);
  orders.push(
    ...market["orders"].sort((a, b) => (a["price"] > b["price"] ? 1 : -1))
  );
  var users = market["users"];
  // create a list of orders by price in html
  exchanges = market["exchanges"];
  marketName = market["marketName"];
  // change innerHTML
  refreshMarket();
});

connection.on("NewOrder", (order) => {
  // console.log(order)
  orders.push(order);
  orders = orders.sort((a, b) => (a["price"] > b["price"] ? 1 : -1));

  refreshMarket();
});

connection.on("DeletedOrder", (orderID) => {
  orders = orders.filter(function (value, index, arr) {
    return value.id != orderID;
  });

  refreshMarket();
});

connection.on("UserJoined", (user) => {
  console.log(user + " joined the market");
});

function updateOrRemove(id, quantity) {
  // get order with ID order.ID
  // update quantity
  var a = orders.findIndex((value) => value.id == id);

  if (a != -1) {
    orders[a]["quantity"] -= Math.sign(orders[a]["quantity"])*quantity;
  }

  if (orders[a]["quantity"] == 0) {
    orders = orders.filter(function (value, index, arr) {
      return value.id != id;
    });
  }
}


connection.on("TransactionEvent", (transactionEvent) => {
  console.log(transactionEvent);
  
  updateOrRemove(transactionEvent["aggressiveOrderId"], transactionEvent["quantityTraded"]);
  updateOrRemove(transactionEvent["passiveOrderId"], transactionEvent["quantityTraded"]);
  refreshMarket();
});

connection.on("ExchangeAdded", (exchangeName) => {
  exchanges.push(exchangeName);
  refreshMarket();
});

// Start the connection.
start();

// ------------------------------

const loadingHtml = `
    <button id="makeMarket">Make Market</button>
    <input type="text" id="joinMakeMarketText" placeholder="Enter market">
    <button id="joinMarket">Join Market</button>
`;

const userHtml = `
    <input type="text" id="nameInput" placeholder="Enter name">
    <button id="joinWithName">Join</button>
    <input type="text" id="exchangeInput" placeholder="Enter exchange">
    <input type="number" id="priceInput" placeholder="Enter price">
    <input type="number" id="quantityInput" placeholder="Enter quantity">
    <button id="send">Send</button>
    <button id="deleteLastOrder">Delete Last Order</button>
`;

const adminHtml = `
    <input type="text" id="newExchangeInput" placeholder="IYE">
    <button id="newExchangeSend">Add Exchange</button> 
`;

function loadUserPage() {
  document.getElementById("commands").innerHTML = userHtml;

  document.getElementById("exchangeInput").value = "A";
  document.getElementById("priceInput").value = 10;
  document.getElementById("quantityInput").value = 4;

  document.getElementById("joinWithName").onclick = () => {
    let name = document.getElementById("nameInput").value;
    connection.invoke("JoinMarket", name);
    document.getElementById("joinWithName").disabled = true;
  };

  // set onclick
  document.getElementById("send").onclick = () => {
    let market = document.getElementById("exchangeInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;
    
    connection
      .invoke("PlaceOrder", market, parseInt(price), parseInt(quantity))
      .catch((err) => console.error(err.toString()));
  };

  document.getElementById("deleteLastOrder").onclick = () => {
    if (orders.length == 0) return;
    
    connection.invoke("DeleteOrder", orders[orders.length - 1]["id"]);
  };
}

function loadAdminPage() {
  document.getElementById("commands").innerHTML = adminHtml;
  document.getElementById("newExchangeInput").value = "A";

  document.getElementById("newExchangeSend").onclick = () => {
    let exchange = document.getElementById("newExchangeInput").value;

    connection.invoke("MakeNewExchange", exchange);
  };
}

document.getElementById("commands").innerHTML = loadingHtml;

document.getElementById("makeMarket").onclick = () => {
  connection.invoke("MakeNewMarket");
  loadAdminPage();
};

document.getElementById("joinMarket").onclick = () => {
  let market = document.getElementById("joinMakeMarketText").value;
  connection.invoke("JoinMarketLobby", market);
  loadUserPage();
};
