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
var orders = {};
var exchanges = [];
var marketName = "";
var transactions = [];
// define function

function refreshMarket() {
  var listorders = [];
  var ordersList = "<nav><ul>";
  Object.keys(orders).forEach(function (key) {
    listorders.push(orders[key]);
  });

  listorders.sort(function (a, b) {
    return a["price"] - b["price"];
  });
  
  for (var i = 0; i < listorders.length; i++) {
    ordersList += "<li>" + formatOrder(listorders[i]) + "</li>";
  }

  ordersList += "</nav></ul>";
  document.getElementById("marketName").innerHTML = "Market Code: " + marketName;
  document.getElementById("exchangeNames").innerHTML =
    "Exchanges: " + exchanges.join(", ");
  document.getElementById("market").innerHTML = ordersList;

  var transactionsList = "<nav><ul>";
  for (var i = 0; i < transactions.length; i++) {
    transactionsList += "<li>" + transactions[i] + "</li>";
  }
  transactionsList += "</nav></ul>";
  document.getElementById("transactions").innerHTML = transactionsList;
}

function formatOrder(order) {
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

  // add all orders to orders dictionary
  for (var i = 0; i < orders.length; i++) {
    orders[orders[i]["id"]] = orders[i];
  }

  // create a list of orders by price in html
  // change innerHTML
  refreshMarket();
});

connection.on("MarketConfig", (message)=> {
  marketName = message["marketName"]
  exchanges = message["exchanges"]
  refreshMarket();
});

connection.on("NewOrder", (order) => {
  console.log(order)
  // console.log("new Order")
  orders[order["id"]] = order;

  refreshMarket();
});

connection.on("DeletedOrder", (orderID) => {
  delete orders[orderID]; 
  refreshMarket();
});

connection.on("UserJoined", (user) => {
  console.log(user + " joined the market");
});

function updateOrRemove(id, quantity)
{
  orders[id]["quantity"] -= Math.sign(orders[id]["quantity"]) * quantity;
  if (orders[id]["quantity"] == 0) {
    delete orders[id];
  }  
}


connection.on("TransactionEvent", (transactionEvent) => {
  
  var action = orders[transactionEvent["aggressiveOrderId"]["quantity"]] > 0 ? "<=" : "=>"; 

  var str = `${transactionEvent["aggressiveOrderId"]} ${action} ${transactionEvent["passiveOrderId"]}`

  transactions.push(str);
  
  updateOrRemove(transactionEvent["aggressiveOrderId"], transactionEvent["quantityTraded"]);
  updateOrRemove(transactionEvent["passiveOrderId"], transactionEvent["quantityTraded"]);
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
  };

  // set onclick
  document.getElementById("send").onclick = () => {
    let market = document.getElementById("exchangeInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;
    console.log("placed order",  market, parseInt(price), parseInt(quantity)); 
    connection
      .invoke("PlaceOrder", market, parseInt(price), parseInt(quantity))
      .catch((err) => console.error(err.toString()));
  };

  document.getElementById("deleteLastOrder").onclick = () => {
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
