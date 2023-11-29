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
let state = "";
let marketCode = "";
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
  document.getElementById("marketName").innerHTML = "Market Name: " + marketName;
  document.getElementById("marketCode").innerHTML = "Market Code: " + marketCode;
  document.getElementById("exchangeNames").innerHTML =
    "Exchanges: " + exchanges.join(", ");
  document.getElementById("market").innerHTML = ordersList;

  var transactionsList = "<nav><ul>";
  for (var i = 0; i < transactions.length; i++) {
    transactionsList += "<li>" + transactions[i] + "</li>";
  }
  transactionsList += "</nav></ul>";
  document.getElementById("transactions").innerHTML = transactionsList;

  document.getElementById("state").innerHTML = "State: " + state;
  // console.log(document.getElementById("state").innerHTML);
}

function formatOrder(order) {
  var date = new Date(Date.parse(order["timeStamp"]));

  const formattedDateString = date.toLocaleString('en-US', {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
  
  const milliseconds = date.getMilliseconds();
  const formattedWithMilliseconds = `${formattedDateString}.${milliseconds}`;

  return (
    order["exchange"] + 
    ") " + 
    formattedWithMilliseconds + 
    ": " +
    " $" +
    order["price"] +
    ", quantity " +
    order["quantity"] +
    " <= " +
    order["user"]
  );
}

function formatTransaction(transactionEvent) {
  var buyer = transactionEvent["buyerUser"] 
  var seller = transactionEvent["sellerUser"]
  
  var buyerIsAgressor = transactionEvent["aggressor"] == buyer;
  var users = buyerIsAgressor ? [buyer, seller] : [seller, buyer];

  var action = buyerIsAgressor ? "bought from" : "sold to"; 
  var str = `${users[0]} ${action} ${users[1]}, ${transactionEvent["quantity"]} @ $${transactionEvent["price"]}`

  return str;
}
connection.onclose(async () => {
  await start();
});

connection.on("ReceiveMessage", (message) => {
  console.log("server: " + message);
});

connection.on("MarketState", (market) => {
  // console.log("marketState", market);
  // add all orders to orders dictionary
  market["orders"].forEach((order) => {
    orders[order["id"]] = order;
  });

  market["transactions"].forEach((transactionEvent) => {
    transactions.push(formatTransaction(transactionEvent));
  });

  state = market["state"];

  refreshMarket();
});

connection.on("StateUpdated", (newState) => {
  state = newState;
  // console.log(newState)
  refreshMarket();
});

connection.on("MarketConfig", (message)=> {
  // console.log("market config", message);  
  marketName = message["marketName"] ?? "[unamed market]";
  exchanges = message["exchanges"]
  refreshMarket();
});

connection.on("MarketCreated", newMarketCode => {
  marketCode = newMarketCode;
  document.getElementById("marketCode").innerHTML = "Market Code: " + marketCode;
  loadAdminPage();
});

connection.on("NewOrder", (order) => {
  // console.log(order)
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
  orders[id]["quantity"] += quantity; 
  if (orders[id]["quantity"] == 0) {
    delete orders[id];
  }  
}


connection.on("TransactionEvent", (transactionEvent) => {

  transactions.push(formatTransaction(transactionEvent));
  
  updateOrRemove(transactionEvent["buyerOrderId"], -transactionEvent["quantity"]);
  updateOrRemove(transactionEvent["sellerOrderId"], transactionEvent["quantity"]);
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
    <button id="joinAsRandom">Join as Random</button>
    <input type="text" id="exchangeInput" placeholder="Enter exchange">
    <input type="number" id="priceInput" placeholder="Enter price">
    <input type="number" id="quantityInput" placeholder="Enter quantity">
    <button id="send">Send</button>
    <button id="deleteLastOrder">Delete Last Order</button>
`;

const adminHtml = `
    <input type="text" id="newExchangeInput" placeholder="IYE">
    <button id="newExchangeSend">Add Exchange</button> 
    <select id="stateList" name="state", onchange = "updateState(this)">
      <option value="inLobby">Lobby</option>
      <option value="open">Open</option>
      <option value="paused">Paused</option>
      <option value="closed">Closed</option>
    </select>
`;

function updateState(element) {

  let newState = element.value;
  connection.invoke("UpdateMarketState", newState);

}

function loadUserPage() {
  document.getElementById("commands").innerHTML = userHtml;

  document.getElementById("exchangeInput").value = "A";
  document.getElementById("priceInput").value = 10;
  document.getElementById("quantityInput").value = 4;

  document.getElementById("joinWithName").onclick = () => {
    let name = document.getElementById("nameInput").value;
    connection.invoke("JoinMarket", name);
  };
  
  document.getElementById("joinAsRandom").onclick = () => {
    let names = ["Tony", "Gil", "Jen", "Kate"];
    let name = names[Math.floor(Math.random() * names.length)];

    document.getElementById("nameInput").value = name;

    connection.invoke("JoinMarket", name);
  };

  // set onclick
  document.getElementById("send").onclick = () => {
    let market = document.getElementById("exchangeInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;
    // console.log("placed order",  market, parseInt(price), parseInt(quantity)); 
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
};

document.getElementById("joinMarket").onclick = () => {
  let market = document.getElementById("joinMakeMarketText").value;
  connection.invoke("JoinMarketLobby", market);
  marketCode = market;
  refreshMarket(); //
  loadUserPage();
};
