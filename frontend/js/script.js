// get the element with id "exchange" and insert a list of items

var orders = {};
var markets = [];
var participants = [];
var exchangeName = "";
var transactions = [];
let state = "";
let exchangeCode = "";
var token = "";

// define function
let serverURL = "https://localhost:7221/";
serverURL = "https://market-maker.azurewebsites.net/";

function refreshExchange() {
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

  document.getElementById("exchange").innerHTML = ordersList;

  var transactionsList = "<nav><ul>";
  for (var i = 0; i < transactions.length; i++) {
    transactionsList += "<li>" + transactions[i] + "</li>";
  }
  transactionsList += "</nav></ul>";
  document.getElementById("transactions").innerHTML = transactionsList;
}

function refreshLobby() {
  document.getElementById("marketNames").innerHTML =
    "Markets: " + markets.join(", ");
  document.getElementById("state").innerHTML = "State: " + state;
  document.getElementById("exchangeCode").innerHTML =
    "Exchange Code: " + exchangeCode;
  document.getElementById("exchangeName").innerHTML =
    "Exchange Name: " + exchangeName;
  let stateList = document.getElementById("stateList")
  if (stateList != null) {
    stateList.value = state.toLowerCase();
  }
}

function formatOrder(order) {
  var date = new Date(Date.parse(order["timeStamp"]));

  const formattedDateString = date.toLocaleString("en-US", {
    hour12: false,
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });

  const milliseconds = date.getMilliseconds();
  const formattedWithMilliseconds = `${formattedDateString}.${milliseconds}`;

  return (
    order["market"] +
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
  var buyer = transactionEvent["buyerUser"];
  var seller = transactionEvent["sellerUser"];
  var market = transactionEvent["market"];
  var buyerIsAgressor = transactionEvent["aggressor"] == buyer;
  var users = buyerIsAgressor ? [buyer, seller] : [seller, buyer];

  var action = buyerIsAgressor
    ? `bought ${market} from`
    : `sold ${market} to`;
  var str = `${users[0]} ${action} ${users[1]}, ${transactionEvent["quantity"]} @ $${transactionEvent["price"]}`;

  return str;
}

function bindConnection(jwt) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(
      serverURL + "exchange", {
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

  connection.on("ExchangeState", (exchange) => {
    console.log("exchangeState", exchange);
    // add all orders to orders dictionary
    exchange["orders"].forEach((order) => {
      orders[order["id"]] = order;
    });

    exchange["transactions"].forEach((transactionEvent) => {
      transactions.push(formatTransaction(transactionEvent));
    });

    refreshExchange();
  });

  connection.on("StateUpdated", (newState) => {
    state = newState;
    // console.log(newState)
    refreshLobby();
  });

  connection.on("LobbyState", (message) => {
    console.log("lobby state", message);

    markets = message.markets.map((codeName) => {
      [code, exchangeName] = codeName;
      return `${code}` + (exchangeName == null ? "" : ` (${exchangeName})`);
    });
    participants = message.participants;
    state = message.state;

    exchangeName = message.exchangeName;

    exchangeCode = message.exchangeCode;

    // console.log("exchange config", message);
    refreshLobby();
  });

  connection.on("NewOrder", (order) => {
    console.log(order)
    // console.log("new Order")
    orders[order["id"]] = order;

    refreshExchange();
  });

  connection.on("DeletedOrder", (orderID) => {
    delete orders[orderID];
    refreshExchange();
  });

  connection.on("NewParticipant", (user) => {
    console.log(user + " joined the exchange");
    refreshLobby();
  });

  function updateOrRemove(id, quantity) {
    orders[id]["quantity"] += quantity;
    if (orders[id]["quantity"] == 0) {
      delete orders[id];
    }
  }

  connection.on("TransactionEvent", (transactionEvent) => {
    transactions.push(formatTransaction(transactionEvent));

    updateOrRemove(
      transactionEvent["buyerOrderId"],
      -transactionEvent["quantity"]
    );
    updateOrRemove(
      transactionEvent["sellerOrderId"],
      transactionEvent["quantity"]
    );
    refreshExchange();
  });

  connection.on("ClosingPrices", (closingPrices) => {
    console.log("closing prices", closingPrices);
  });

  return [connection, start];
}

// Start the connection.
// ------------------------------

const loadingHtml = `
    <button id="makeExchange">Make Exchange</button>
    <input type="text" id="joinMakeExchangeText" placeholder="Enter exchange">
    <button id="joinExchange">Join Exchange</button>
`;

const userHtml = `
    <input type="text" id="nameInput" placeholder="Enter name">
    <button id="joinWithName">Join</button>
    <button id="joinAsRandom">Join as Random</button>
    <input type="text" id="marketInput" placeholder="Enter market">
    <input type="number" id="priceInput" placeholder="Enter price">
    <input type="number" id="quantityInput" placeholder="Enter quantity">
    <button id="send">Send</button>
    <button id="deleteLastOrder">Delete Last Order</button>
`;

const adminHtml = `
    <button id="newMarketSend">Add Market</button> 
    <select id="stateList" name="state">
      <option value="lobby">Lobby</option>
      <option value="open">Open</option>
      <option value="paused">Paused</option>
      <option value="closed">Closed</option>
    </select>
    <button id="updateConfig">Update Config</button>
    <input type="number" id="closeExchangeInput" placeholder="Enter closing price">
    <button id="closeExchange">Close Exchange</button>
    <div>
      <button id="Serialize">Serialize</button>
      <input type="file" id="fileInput" accept=".json">
      <button id="loadJson">Load Exchange</button>
    </div>
`;

function loadUserPage(connection) {
  document.getElementById("commands").innerHTML = userHtml;

  document.getElementById("marketInput").value = "A";
  document.getElementById("priceInput").value = 10;
  document.getElementById("quantityInput").value = 4;

  document.getElementById("joinWithName").onclick = () => {
    let name = document.getElementById("nameInput").value;
    connection.invoke("JoinExchange", name);
  };

  document.getElementById("joinAsRandom").onclick = () => {
    let names = ["Tony", "Gil", "Jen", "Kate"];
    let name = names[Math.floor(Math.random() * names.length)];

    document.getElementById("nameInput").value = name;

    connection.invoke("JoinExchange", name);
  };

  // set onclick
  document.getElementById("send").onclick = () => {
    let exchange = document.getElementById("marketInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;
    // console.log("placed order",  exchange, parseInt(price), parseInt(quantity));
    connection
      .invoke("PlaceOrder", exchange, parseInt(price), parseInt(quantity))
      .catch((err) => console.error(err.toString()));
  };

  document.getElementById("deleteLastOrder").onclick = () => {
    user = document.getElementById("nameInput").value;

    var order = Object.values(orders).filter(order => order.user == user.toLowerCase())[0]
    console.log(order)

    if (order == null) {
      console.log("no orders to delete");
      return;
    }

    connection.invoke("DeleteOrder", order.id);
  };
}

function loadAdminPage(connection) {
  document.getElementById("commands").innerHTML = adminHtml;

  document.getElementById("stateList").onchange = () => {
    let newState = document.getElementById("stateList").value;
    console.log("new state", newState);
    connection.invoke("UpdateExchangeState", newState);
  }

  document.getElementById("newMarketSend").onclick = () => {
    connection.invoke("MakeNewMarket");
  };

  document.getElementById("updateConfig").onclick = () => {
    // console.log("update config");
    config = {
      exchangeName: "Test Exchange",
      marketNames: {
        "A": "Bikes",
        "B": "Cars"
      },
    };
    connection.invoke("UpdateConfig", config);
  };
  // var closingPrice = document.getElementById("closeExchangeInput").value ? parseInt(document.getElementById("closeExchangeInput").value) : null;

  document.getElementById("closeExchange").onclick = () => {
    var closingPrice = {
      A: 10,
    };

    closingPrice = null;
    connection.invoke("CloseExchange", closingPrice);

  };

  document.getElementById("Serialize").onclick = () => {
    connection.invoke("Serialize");
  };

  document.getElementById("loadJson").onclick = () => {
    var file = document.getElementById("fileInput").files[0];
    var reader = new FileReader();
    reader.onload = function (e) {
      var text = reader.result;
      connection.invoke("LoadExchange", text);
    };
    reader.readAsText(file);
  }
}
document.getElementById("commands").innerHTML = loadingHtml;

document.getElementById("login").onclick = async () => {
  const email = document.getElementById("email").value;
  const password = document.getElementById("password").value;

  const rawResponse = await fetch(serverURL + "login", {
    method: 'POST',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({Email: email, Password: password})
  });
  console.log(rawResponse)
  if (!rawResponse.ok) return;
  token = await rawResponse.text()
  token = token.replace("\"", "").replace("\"", "")
  document.getElementById("login").disabled = true;
}

document.getElementById("makeExchange").onclick = async () => {
  const data = await fetch(serverURL + "createExchange", {
    method: "GET",
    headers : {
      "Authorization": "Bearer " + token,
      "Content-Type": "application/json"
      }
    }
  )

  if (!data.ok) {
    console.log("you must be logged in to create an exchange")
    return;
  }
  let jwt = data.text();

  let [connection, start] = bindConnection(jwt);
  start().then(() => {
    loadAdminPage(connection);
  });
};

document.getElementById("joinExchange").onclick = async () => {
    let exchange = document.getElementById("joinMakeExchangeText").value;
    if (exchange == "") return;

    const data = await fetch(serverURL + "joinExchange?exchangeCode=" + exchange);
    if (!data.ok) return;
    let jwt = data.text();

    let [connection, start] = bindConnection(jwt);
    start().then(() => {
      loadUserPage(connection);
    })

    refreshExchange(); //
  };
