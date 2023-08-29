


// get the element with id "market" and insert a list of items
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7221/market", {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
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
};
var orders = []

// define function
function refreshMarket() {
    var ordersList = '<ul>';
    for (var i = 0; i < orders.length; i++) {
        ordersList += '<li>' + formatOrder(orders[i]) + '</li>';
    }
    ordersList += '</ul>';
    document.getElementById("market").innerHTML = ordersList;
}

function formatOrder(order) {
    console.log(order)
    return order["createdAt"] + ": " + " $" + order["price"] + ', quantity ' + order["quantity"] + " <= " + order["user"].slice(0, 3)
}

connection.onclose(async () => {
    await start();
});


connection.on("MarketState", market => {
    console.log(market)
    orders.push(...market["orders"].sort((a, b) => (a["price"] > b["price"]) ? 1 : -1));
    var users = market["users"];
    // create a list of orders by price in html
    

    // change innerHTML
    refreshMarket();
    
})

connection.on("NewOrder", (order) => {
    // console.log(order)
    orders.push(order);
    orders = orders.sort((a, b) => (a["price"] > b["price"]) ? 1 : -1);

    refreshMarket();
})

connection.on("DeletedOrder", (order) => {
    orders = orders.filter(function(value, index, arr){
        return value.id != order.id;
    });

    refreshMarket();  
})

connection.on("UserJoined", user => {
    console.log(user + ' joined the market')
})

connection.on("RecieveMessage", (message) => {
    console.log(message)
})

connection.on("OrderFilled", (order) => {

    if (order["newQuantity"] == 0) {
        
        orders = orders.filter(function(value, index, arr){
            return value.id.toString() != order["id"];
        });
    }
    else {
        // get order with ID order.ID
        // update quantity
        var a = orders.findIndex( value => value.id == order["id"])

        if (a != -1) {
            orders[a]["quantity"] =  order["newQuantity"];
        }

    }
    refreshMarket(); 
}
)

// Start the connection.
start();



// ------------------------------
document.getElementById("marketInput").value = "IYE"
document.getElementById("priceInput").value = 10
document.getElementById("quantityInput").value = 4
// get value from input fields
function getValues() {
    let market = document.getElementById("marketInput").value;
    let price = document.getElementById("priceInput").value;
    let quantity = document.getElementById("quantityInput").value;

    market = "IYE";
    const order = {
        Market: market,
        Price: price,
        Quantity: quantity
    }
    connection.invoke("PlaceOrder", market, parseInt(price), parseInt(quantity)).catch(err => console.error(err.toString()));
    
}

// set onclick
document.getElementById("send").onclick = getValues