// get the element with id "market" and insert a list of items
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7221/market", {
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
  document.getElementById("marketName").innerHTML = "Market: " + marketName;
  document.getElementById("exchangeNames").innerHTML =
    "Exchanges: " + exchanges.join(", ");
  document.getElementById("market").innerHTML = ordersList;
}

function formatOrder(order) {
  console.log(order);
  return (
    order["createdAt"] +
    ": " +
    " $" +
    order["price"] +
    ", quantity " +
    order["quantity"] +
    " <= " +
    order["user"].slice(0, 3)
  );
}

connection.onclose(async () => {
  await start();
});

connection.on("MarketState", (market) => {
  console.log(market);
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

connection.on("DeletedOrder", (order) => {
  orders = orders.filter(function (value, index, arr) {
    return value.id != order.id;
  });

  refreshMarket();
});

connection.on("UserJoined", (user) => {
  console.log(user + " joined the market");
});

connection.on("RecieveMessage", (message) => {
  console.log(message);
});

connection.on("OrderFilled", (order) => {
  if (order["newQuantity"] == 0) {
    orders = orders.filter(function (value, index, arr) {
      return value.id.toString() != order["id"];
    });
  } else {
    // get order with ID order.ID
    // update quantity
    var a = orders.findIndex((value) => value.id == order["id"]);

    if (a != -1) {
      orders[a]["quantity"] = order["newQuantity"];
    }
  }
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
    <input type="text" id="joinMakeMarketText" placeholder="Enter market">
    <button id="joinMarket">Join Market</button>
    <button id="makeMarket">Make Market</button>
`;

const userHtml = `
    <input type="text" id="exchangeInput" placeholder="Enter exchange">
    <input type="number" id="priceInput" placeholder="Enter price">
    <input type="number" id="quantityInput" placeholder="Enter quantity">
    <button id="send">Send</button>
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

  // set onclick
  document.getElementById("send").onclick = () => {
    let market = document.getElementById("exchangeInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;

    // market = "IYE";
    const order = {
      Market: market,
      Price: price,
      Quantity: quantity,
    };
    connection
      .invoke("PlaceOrder", market, parseInt(price), parseInt(quantity))
      .catch((err) => console.error(err.toString()));
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
  let market = document.getElementById("joinMakeMarketText").value;
  connection.invoke("MakeNewMarket", market);
  loadAdminPage();
};

document.getElementById("joinMarket").onclick = () => {
  let market = document.getElementById("joinMakeMarketText").value;
  connection.invoke("JoinMarket", market);
  loadUserPage();
};
