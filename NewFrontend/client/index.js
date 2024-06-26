import { Exchange } from "./exchange";
import { Player } from "./player";

let positionLimit = 1000;
let updateDelay = 500; // number of miliseconds after deleting that the frontend updates

// let exchange = {
//     markets: {}, // dictionary of market codes to dictionary of orders and transactions
//     participants: [],
//     state: "Lobby",
//     exchangeName: "",
//     exchangeCode: "",
// };

// const serverURL = "https://market-maker.azurewebsites.net/";

marketToTabID = {} // used to identify the content tab ID for a given market (to use for populating the tab content)

let transaction_cols = ["Price", "Quantity", "Buyer", "Seller", "Aggressive", "Passive"];
let filter_ranges = {"Selector": ""};
for(let col of transaction_cols){
    filter_ranges[col] = ["", ""];
}

function initOrderInput(){
    Object.keys(exchange.markets).forEach((marketKey) => {
        let d = document.getElementById(`orderInput-${marketToTabID[marketKey]}`);
        d.innerHTML='';
        d.classList.add("w-2/3", "min-h-[50%]", "gap-[2%]", "border", "border-black");

        quickInput = document.createElement("input");
        quickInput.classList.add("h-[33%]", "w-[100%]", "border", "border-black", "text-center", "text-lg");
        quickInput.placeholder = "Quick Input";
        quickInput.addEventListener("keydown", function(event){
            if(event.key === "Enter"){
                if(this.value.match(/^[0-9]*[ba][0-9]+$/) || this.value.match(/^[c][0-9]+$/)){
                    // if quantity number exists use it otherwise set to 1
                    let action = this.value.match(/[abc]/)[0];
                    let price = this.value.match(/[0-9]+$/)[0];
                    if (action == "c") {
                        // cancel all orders at that price
                        let name = document.getElementById("playerName").textContent;
                        let myOrders = exchange.markets[marketKey].orders.filter((o) => o.user == name && o.price == price)
                        for(let myOrder in myOrders){
                            connection
                                .invoke("DeleteOrder", myOrders[myOrder].id)
                                .catch((err) => console.error(err.toString()));
                        }
                        setTimeout(() => {
                            exchange.markets[marketKey].orders = exchange.markets[marketKey].orders.filter((o) => o.user != name || o.price != price);
                            updateContent();
                        }, updateDelay);
                    }
                    else {
                        let quantity = this.value.match(/^[0-9]+/) ? parseInt(this.value.match(/^[0-9]+/)[0]) : 1;
                        quantity = action == "b" ? quantity : -quantity;
                        if(!isNaN(parseInt(price))){    
                            if (positionExceeded(quantity, marketKey)) {
                                // flash the position div red
                                document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.add("!bg-red-500");
                                setTimeout(() => {
                                    document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.remove("!bg-red-500");
                                }, 500);
                            }
                            else if(action == "b"){
                                connection
                                    .invoke("PlaceOrder", marketKey, parseInt(price), quantity, "ref")
                                    .catch((err) => console.error(err.toString()));
                                console.log(`bid ${parseInt(price)}`);

                            } else if(action == "a"){
                                connection
                                    .invoke("PlaceOrder", marketKey, parseInt(price), quantity, "ref")
                                    .catch((err) => console.error(err.toString()));
                                console.log(`ask ${parseInt(price)}`);
                            }
                        }
                    }
                }   
                this.value = "";
            }
        });
        quickInput.addEventListener("input", function(event){
            // remove invalid characters
            this.value = this.value.replace(/[^0-9abc]/g, "");
            // can only have one a, b, or c at a time
            if(this.value.match(/[abc]/g) && this.value.match(/[abc]/g).length > 1){
                this.value = this.value.slice(0, -1);
            }
            // if it matches regex set font medium
            if(this.value.match(/^[0-9]*[ba][0-9]+$/) || this.value.match(/^[c][0-9]+$/)) {
                this.classList.add("!font-medium");
            } else {
                this.classList.remove("!font-medium");

            }
        });

        inputDiv = document.createElement("div");
        inputDiv.classList.add("h-[33%]", "flex");

        bidDiv = document.createElement("div");
        bidDiv.classList.add("h-[100%]", "w-[50%]", "border", "border-black");

        bidPriceInput = document.createElement("input");
        bidPriceInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        bidPriceInput.type = "number";
        bidPriceInput.placeholder = "Price";
        bidPriceInput.id = `bidPriceInput-${marketToTabID[marketKey]}`;
        bidPriceInput.addEventListener("input", function(event){
            this.value = Math.abs(parseInt(this.value));
        });

        bidQuantityInput = document.createElement("input");
        bidQuantityInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        bidQuantityInput.type = "number";
        bidQuantityInput.placeholder = "Quantity";
        bidQuantityInput.id = `bidQuantityInput-${marketToTabID[marketKey]}`;
        bidQuantityInput.addEventListener("input", function(event){
            this.value = Math.abs(parseInt(this.value));
        });
        
        bidButtonDiv = document.createElement("div");
        bidButtonDiv.classList.add("h-[50%]", "w-[100%]", "flex", "justify-center", "items-center");

        bidButton = document.createElement("button");
        bidButton.classList.add("h-[70%]", "w-[70%]", "text-center", "text-sm", "font-medium", "bg-green-600", "hover:bg-green-700", "text-white", "rounded-md");
        bidButton.innerHTML = "Send Bid";
        bidButton.id = `bidButton-${marketToTabID[marketKey]}`;
        bidButton.addEventListener("click", function(event){
            let price = document.getElementById(`bidPriceInput-${marketToTabID[marketKey]}`).value;
            let quantity = document.getElementById(`bidQuantityInput-${marketToTabID[marketKey]}`).value;
            if (positionExceeded(parseInt(quantity), marketKey)) {
                // flash the position div red
                document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.add("!bg-red-500");
                setTimeout(() => {
                    document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.remove("!bg-red-500");
                }, 500);
            }
            else {
                console.log(`Send bid of ${price} @ ${quantity}`);
                connection
                    .invoke("PlaceOrder", marketKey, parseInt(price), parseInt(quantity), "ref")
                    .catch((err) => console.error(err.toString()));
            }
        });

        askDiv = document.createElement("div");
        askDiv.classList.add("h-[100%]", "w-[50%]", "border", "border-black");

        askPriceInput = document.createElement("input");
        askPriceInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        askPriceInput.type = "number";
        askPriceInput.placeholder = "Price";
        askPriceInput.id = `askPriceInput-${marketToTabID[marketKey]}`;
        askPriceInput.addEventListener("input", function(event){
            this.value = Math.abs(parseInt(this.value));
        });

        askQuantityInput = document.createElement("input");
        askQuantityInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        askQuantityInput.type = "number";
        askQuantityInput.placeholder = "Quantity";
        askQuantityInput.id = `askQuantityInput-${marketToTabID[marketKey]}`;
        askQuantityInput.addEventListener("input", function(event){
            this.value = Math.abs(parseInt(this.value));
        });

        askButtonDiv = document.createElement("div");
        askButtonDiv.classList.add("h-[50%]", "w-[100%]", "flex", "justify-center", "items-center");

        askButton = document.createElement("button");
        askButton.classList.add("h-[70%]", "w-[70%]", "text-center", "text-sm", "font-medium", "bg-red-600", "hover:bg-red-800", "text-white", "rounded-md");
        askButton.innerHTML = "Send Ask";
        askButton.id = `askButton-${marketToTabID[marketKey]}`;
        askButton.addEventListener("click", function(event){
            let price = document.getElementById(`askPriceInput-${marketToTabID[marketKey]}`).value;
            let quantity = document.getElementById(`askQuantityInput-${marketToTabID[marketKey]}`).value;
            if (positionExceeded(-parseInt(quantity), marketKey)) {
                // flash the position div red
                document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.add("!bg-red-500");
                setTimeout(() => {
                    document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.remove("!bg-red-500");
                }, 500);
            }
            else {
                console.log(`Send ask of ${price} @ ${quantity}`);
                connection
                .invoke("PlaceOrder", marketKey, parseInt(price), -parseInt(quantity), "ref")
                .catch((err) => console.error(err.toString()));
            }
        });

        cancelBids = document.createElement("button");
        cancelBids.classList.add("h-[17%]", "w-[50%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelBids.innerHTML = "Cancel Bids";
        cancelBids.addEventListener("click", function(event){
            let name = document.getElementById("playerName").textContent;
            let myBids = exchange.markets[marketKey].orders.filter((o) => o.user == name && o.quantity > 0)
            for(let myBid in myBids){
                connection
                    .invoke("DeleteOrder", myBids[myBid].id)
                    .catch((err) => console.error(err.toString()));
            }
            setTimeout(() => {
                exchange.markets[marketKey].orders = exchange.markets[marketKey].orders.filter((o) => o.user != name || o.quantity < 0);
                updateContent();
            }, updateDelay);
            console.log("Cancel Bids");
        });

        cancelAsks = document.createElement("button");
        cancelAsks.classList.add("h-[17%]", "w-[50%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelAsks.innerHTML = "Cancel Asks";
        cancelAsks.addEventListener("click", function(event){
            let name = document.getElementById("playerName").textContent;
            let myAsks = exchange.markets[marketKey].orders.filter((o) => o.user == name && o.quantity < 0)
            for(let myAsk in myAsks){
                connection
                    .invoke("DeleteOrder", myAsks[myAsk].id)
                    .catch((err) => console.error(err.toString()));
            }
            setTimeout(() => {
                exchange.markets[marketKey].orders = exchange.markets[marketKey].orders.filter((o) => o.user != name || o.quantity > 0);
                updateContent();
            }, updateDelay);
            console.log("Cancel Asks");
        });

        cancelAll = document.createElement("button");
        cancelAll.classList.add("h-[17%]", "w-[100%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelAll.innerHTML = "Cancel All";
        cancelAll.addEventListener("click", function(event){
            let name = document.getElementById("playerName").textContent;
            let myOrders = exchange.markets[marketKey].orders.filter((o) => o.user == name)
            for(let myOrder in myOrders){
                connection
                    .invoke("DeleteOrder", myOrders[myOrder].id)
                    .catch((err) => console.error(err.toString()));
            }
            setTimeout(() => {
                exchange.markets[marketKey].orders = exchange.markets[marketKey].orders.filter((o) => o.user != name);
                updateContent();
            }, updateDelay);
            console.log("Cancel All");
        });

        bidDiv.appendChild(bidPriceInput);
        bidDiv.appendChild(bidQuantityInput);
        bidDiv.appendChild(bidButtonDiv);
        bidButtonDiv.appendChild(bidButton);
        askDiv.appendChild(askPriceInput);
        askDiv.appendChild(askQuantityInput);
        askDiv.appendChild(askButtonDiv);
        askButtonDiv.appendChild(askButton);
        inputDiv.appendChild(bidDiv);
        inputDiv.appendChild(askDiv)
        d.appendChild(quickInput);
        d.appendChild(inputDiv);
        d.appendChild(cancelBids);
        d.appendChild(cancelAsks);
        d.appendChild(cancelAll);

    });
}

function initProductView(){
    Object.keys(exchange.markets).forEach((marketKey) => {

        let d = document.getElementById(`productView-${marketToTabID[marketKey]}`);
        d.innerHTML='';
        d.classList.add("w-1/3", "min-h-[50%]", "gap-[2%]", "border", "border-black");
        let positionDiv = document.createElement("div");
        positionDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let position_text = document.createElement("p");
        // position_text.classList.add("h-[10%]", "w-[100%]", "text-center", "border", "border-black", "flex", "items-center", "justify-center");
        position_text.innerHTML = "POSITION";
        let position = document.createElement("p");
        // position.classList.add("h-[10%]", "w-[100%]", "text-center");
        position.innerHTML = 0;
        position.id = `position-${marketToTabID[marketKey]}`;

        let bnsDiv = document.createElement("div");
        bnsDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "items-center", "justify-center");

        let buysDiv = document.createElement("div");
        buysDiv.classList.add("h-[100%]", "w-[50%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let buys_text = document.createElement("p");
        buys_text.classList.add("w-[100%]", "text-center");
        buys_text.innerHTML = "BUYS";
        let buys = document.createElement("p");
        buys.classList.add("w-[100%]", "text-center");
        buys.innerHTML = 0;
        buys.id = `buys-${marketToTabID[marketKey]}`;

        let sellsDiv = document.createElement("div");
        sellsDiv.classList.add("h-[100%]", "w-[50%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let sells_text = document.createElement("p");
        sells_text.classList.add("w-[100%]", "text-center");
        sells_text.innerHTML = "SELLS";
        let sells = document.createElement("p");
        sells.classList.add("w-[100%]", "text-center");
        sells.innerHTML = 0;
        sells.id = `sells-${marketToTabID[marketKey]}`;

        let cashDiv = document.createElement("div");
        cashDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let cash_text = document.createElement("p");
        cash_text.classList.add("w-[100%]", "text-center");
        cash_text.innerHTML = "CASH";
        let cash = document.createElement("p");
        cash.classList.add("w-[100%]", "text-center");
        cash.innerHTML = 0;
        cash.id = `cash-${marketToTabID[marketKey]}`;

        let settlementDiv = document.createElement("div");
        settlementDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let settlement_text = document.createElement("p");
        settlement_text.classList.add("w-[100%]", "text-center");
        settlement_text.innerHTML = "SETTLEMENT";
        let settlement = document.createElement("p");
        settlement.classList.add("w-[100%]", "text-center");
        settlement.innerHTML = "????";
        settlement.id = `settlement-${marketToTabID[marketKey]}`;
        
        positionDiv.appendChild(position_text);
        positionDiv.appendChild(position);
        d.appendChild(positionDiv);
        buysDiv.appendChild(buys_text);
        buysDiv.appendChild(buys);
        sellsDiv.appendChild(sells_text);
        sellsDiv.appendChild(sells);
        bnsDiv.appendChild(buysDiv);
        bnsDiv.appendChild(sellsDiv);
        d.appendChild(bnsDiv);
        cashDiv.appendChild(cash_text);
        cashDiv.appendChild(cash);
        d.appendChild(cashDiv);
        settlementDiv.appendChild(settlement_text);
        settlementDiv.appendChild(settlement);
        d.appendChild(settlementDiv);
    });
}

function initTransactionTable(){
    document.getElementById("exchangeName").textContent = exchange.exchangeName;
    document.getElementById("exchangeCode").textContent = exchange.exchangeCode;
    document.getElementById("exchangeState").textContent = exchange.state;
    // console.log(exchange.markets);

    // update transactions
    Object.keys(exchange.markets).forEach((marketKey) => {
        // console.log(marketKey);
        let table = document.getElementById(`transactions-${marketToTabID[marketKey]}`);
        table.innerHTML = ''; // erase existing content
        table.classList.add("w-full");
        let thead = table.createTHead();
        thead.classList.add("sticky", "top-0");
        let header = thead.insertRow();
        
        for (let col of transaction_cols) {
            let cell = header.insertCell();
            cell.outerHTML = `<th>${col}</th>`;
            cell.textContent = col;
        }
        header.classList.add("[&>*]:px-2", "bg-gray-200");

        let border = thead.insertRow();
        border.classList.add();
        cell = document.createElement("th");
        cell.colSpan = 6;
        cell.classList.add("h-[2px]", "bg-gray-500");
        border.appendChild(cell);

        tbody = table.createTBody();

        let filters = thead.insertRow();
        filters.classList.add("bg-gray-200");
        for (let col of transaction_cols) {
            let cell = filters.insertCell();    
            
            let select = document.createElement("select");
            options = [];
            if (col == "Price" || col == "Quantity"){
                options.push("");
                options.push("=");
                options.push("≥");
                options.push("≤");
                for (let option of options) {
                    let opt = document.createElement("option");
                    opt.value = option;
                    opt.text = option;
                    select.appendChild(opt);
                }
                cell.appendChild(select);
            }
            
            select.style.width = "35px";
            select.style.display = "inline-block";
            // select.classList.add("filter-select");
            select.id = `filter-select-${col}`;
            select.addEventListener("change", function(event){
                filter_ranges[col][0] = event.target.value;
                updateContent();
            });

            let input = document.createElement("input");
            if(col == "Price" || col == "Quantity"){
                input.type = "number";
                input.classList.add("w-[calc(100%-35px)]", "text-center");

            }else{
                input.type = "text";
                input.classList.add("w-full", "text-center");
            }
            input.placeholder = col; 
            
            // input.classList.add("filter-input");
            input.id = `filter-input-${col}`;
            input.addEventListener("input", function(event){
                if(col == "Price" || col == "Quantity"){
                    filter_ranges[col][1] = Math.round(parseFloat(event.target.value));
                }else{
                    filter_ranges[col][0] = "="
                    filter_ranges[col][1] = event.target.value;
                }
                updateContent();
            });

            cell.appendChild(input);
        }
        updateContent();
    });
}

// updates all the content on the page
function updateContent() {

    // // update transactions
    Object.keys(exchange.markets).forEach((marketKey) => {
        let table = document.getElementById(`transactions-${marketToTabID[marketKey]}`);
        if(table.getElementsByTagName("tbody").length > 0){
            tbody = table.tBodies[0];
            for (let i = tbody.rows.length - 1; i >= 0; i--) {
                tbody.deleteRow(i);
            }
        }

        let buy_count = 0;
        let sell_count = 0;
        let cash_count = 0;
        let name = document.getElementById("playerName").textContent;

        Object.keys(exchange.markets[marketKey].transactions).forEach((transactionKey) => {
            // console.log(transactionKey);
            let transaction = exchange.markets[marketKey].transactions[transactionKey];

            if(transaction.buyerUser == name){
                buy_count += transaction.quantity;
                cash_count -= transaction.price * transaction.quantity;
            }
            if(transaction.sellerUser == name){
                sell_count += transaction.quantity;
                cash_count += transaction.price * transaction.quantity;
            }

            document.getElementById(`position-${marketToTabID[marketKey]}`).innerHTML = buy_count - sell_count;
            document.getElementById(`buys-${marketToTabID[marketKey]}`).innerHTML = buy_count;
            document.getElementById(`sells-${marketToTabID[marketKey]}`).innerHTML = sell_count;
            document.getElementById(`cash-${marketToTabID[marketKey]}`).innerHTML = cash_count;

            let passive = transaction.sellerUser;
            let aggressive = transaction.buyerUser;
            if(transaction.passiveOrder == transaction.buyerOrderId){
                passive = transaction.buyerUser;
                aggressive = transaction.sellerUser;
            }
            transaction_dict = {
                "Price": transaction.price,
                "Quantity": transaction.quantity,
                "Buyer": transaction.buyerUser,
                "Seller": transaction.sellerUser,
                "Aggressive": aggressive,
                "Passive": passive
            }
            
            
            let reject = false;
            let row = document.createElement("tr");
            Object.keys(transaction_dict).forEach((col) => {
                if(filter_ranges[col][0] != "" && filter_ranges[col][1] != ""){
                    if(filter_ranges[col][0] == "=" && !transaction_dict[col].toString().includes(filter_ranges[col][1])){
                        reject = true;
                    }
                    if(filter_ranges[col][0] == "≥" && transaction_dict[col] < filter_ranges[col][1]){
                        reject = true;
                    }
                    if(filter_ranges[col][0] == "≤" && transaction_dict[col] > filter_ranges[col][1]){
                        reject = true;
                    }
                }
                

                let cell = row.insertCell();
                if (["Buyer", "Seller", "Aggressive", "Passive"].includes(col)){
                    cell.addEventListener("click", function(event){
                        if (filter_ranges["Selector"] == event.target.textContent){
                            filter_ranges["Selector"] = "";
                        } else {
                            filter_ranges["Selector"] = event.target.textContent;
                        }
                        console.log(filter_ranges);
                        updateContent();
                    });
                    cell.classList.add("cursor-pointer", "hover:bg-gray-300");
                    if (transaction_dict[col] == filter_ranges["Selector"]){
                        cell.classList.add("bg-gray-300");
                    }
                }
                cell.textContent = transaction_dict[col];
            });
            // check for every of the 4 columns if none have the selector value then reject is true
            if(filter_ranges["Selector"] != ""){
                if(["Buyer", "Seller", "Aggressive", "Passive"].every((col) => transaction_dict[col] != filter_ranges["Selector"]) ){
                    reject = true;
                }
            }
            if(!reject){
                tbody.appendChild(row);
            }
            row.classList.add("border-b", "border-gray-400", "[&>*]:px-2");
        });

    });

    // update orders
    Object.keys(exchange.markets).forEach((marketKey) => {
        // console.log(marketKey);
        let table = document.getElementById(`orders-${marketToTabID[marketKey]}`);
        table.innerHTML = ''; // erase existing content
        table.classList.add("w-full");
        let thead = table.createTHead();
        thead.classList.add("sticky", "top-0");
        let header = thead.insertRow();
        for (let col of ["Bidder", "Quantity", "Price", "Quantity", "Asker"]) {
            let cell = header.insertCell();
            cell.outerHTML = `<th>${col}</th>`;
            cell.textContent = col;
        }
        header.classList.add("[&>*]:px-2", "bg-gray-200");

        let border = thead.insertRow();
        border.classList.add();
        cell = document.createElement("th");
        cell.colSpan = 6;
        cell.classList.add("h-[2px]", "bg-gray-500");
        border.appendChild(cell);

        tbody = table.createTBody();

        // add the orders

        let minPrice = Math.min(...exchange.markets[marketKey].orders.map((o) => o.price));
        let maxPrice = Math.max(...exchange.markets[marketKey].orders.map((o) => o.price));

        // sort the orders by price descending
        exchange.markets[marketKey].orders.sort((a, b) => b.price - a.price);
        
        for (let order of exchange.markets[marketKey].orders) {
            let row = tbody.insertRow();
            let cell = row.insertCell();
            cell.textContent = order.quantity > 0 ? order.user : "";
            cell = row.insertCell();
            cell.textContent = order.quantity > 0 ? order.quantity : "";
            cell = row.insertCell();
            cell.textContent = order.price;
            cell = row.insertCell();
            cell.textContent = order.quantity < 0 ? - order.quantity  : "";
            cell = row.insertCell();
            cell.textContent = order.quantity < 0 ? order.user : "";
            row.classList.add("border-b", "border-gray-400", "[&>*]:px-2", "hover:bg-gray-300");
            // on double click instantly trade with this order
            row.addEventListener("dblclick", function(event){
                // check if I am the person who made order
                if (document.getElementById("playerName").textContent != order.user){
                    let price = order.price;
                    let quantity = -order.quantity;
                    let market = marketKey;
                    if (positionExceeded(parseInt(quantity), marketKey)) {
                        // flash the position div red
                        document.getElementById(`pb080osition-${marketToTabID[marketKey]}`).parentElement.classList.add("!bg-red-500");
                        setTimeout(() => {
                            document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.remove("!bg-red-500");
                        }, 500);
                    } else {
                        console.log(`Send trade of ${price} @ ${quantity}`);
                        connection
                            .invoke("PlaceOrder", market, parseInt(price), parseInt(quantity), "ref")
                            .catch((err) => console.error(err.toString()));
                    }
                } else {
                    console.log("DeleteOrder", order);
                    // cancel order
                    connection
                        .invoke("DeleteOrder", order.id)
                        .catch((err) => console.error(err.toString()));
                    // delete it from the list
                    setTimeout(() => {
                        exchange.markets[marketKey].orders = exchange.markets[marketKey].orders.filter((o) => o.id != order.id);
                        updateContent();
                    }, updateDelay);
                }
            });
        }

    });
}

function updateModal() {
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
function generateTabs(tabNames) {
    const tabNavigation = document.getElementById('tab-navigation');
    const tabContent = document.getElementById('tab-content');

    const activeTabColor = "bg-gray-200";
    const inactiveTabColor = "bg-gray-100";
    // Clear existing tabs and content
    tabNavigation.innerHTML = '';
    tabContent.innerHTML = '';
    
    marketToTabID = tabNames.reduce((acc, name, index) => {
        acc[name] = index; // create a dictionary of market names to tab content IDs
        return acc;
    }, {});
    // Create tabs and content dynamically
    tabNames.forEach((name, index) => {
        const tabId = `tab-${index}`;
        const contentId = `content-${index}`;

        // Create tab element
        const tab = document.createElement('div');
        tab.id = tabId;
        tab.classList.add('tab', inactiveTabColor, 'hover:'+activeTabColor, 'px-4', 'rounded-t-md', 'cursor-pointer');
        tab.textContent = exchange.market_names[name];
        tabNavigation.appendChild(tab);

        // Create content element
        const content = document.createElement('div');
        content.id = contentId;
        content.classList.add('p-4', 'bg-gray-200', 'hidden', 'h-full', 'flex', 'rounded-br-lg', 'rounded-bl-lg', 'rounded-tr-lg');

        const leftSide = document.createElement('div');
        leftSide.classList.add('w-1/2', 'h-full', 'pl-2', "pr-4", "overflow-y-auto");

        const orderTable = document.createElement('table');
        orderTable.id = `orders-${index}`;

        leftSide.appendChild(orderTable);

        const rightSide = document.createElement('div');
        rightSide.classList.add('w-1/2', 'h-full', "pr-2", "pl-4");

        const topRightSide = document.createElement('div');
        topRightSide.classList.add('w-full', 'h-1/2', "overflow-y-auto", "flex");

        const bottomRightSide = document.createElement('div');
        bottomRightSide.classList.add('w-full', 'h-1/2', "overflow-y-auto");

        const orderInput = document.createElement('div');
        orderInput.id = `orderInput-${index}`;

        const productView = document.createElement('div');
        productView.id = `productView-${index}`;

        const transactionTable = document.createElement('table');
        transactionTable.id = `transactions-${index}`;

        topRightSide.appendChild(orderInput);
        topRightSide.appendChild(productView);
        bottomRightSide.appendChild(transactionTable);

        rightSide.appendChild(topRightSide);
        rightSide.appendChild(bottomRightSide);

        content.appendChild(leftSide);
        content.appendChild(rightSide);

        tabContent.appendChild(content);

        // Click handler for tabs
        tab.addEventListener('click', () => {
            const allContent = document.querySelectorAll('[id^="content-"]');
            allContent.forEach(item => item.classList.add('hidden'));
            document.getElementById(contentId).classList.remove('hidden');
            const allTabs = document.querySelectorAll('[id^="tab-"]');
            allTabs.forEach(item => item.classList.remove(activeTabColor));
            allTabs.forEach(item => item.classList.add(inactiveTabColor));
            document.getElementById(tabId).classList.remove(inactiveTabColor);
            document.getElementById(tabId).classList.add(activeTabColor);

        });
    });

    // Show the first tab initially
    tabNavigation.firstChild.click();
    initOrderInput();
    initProductView();
    initTransactionTable();
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

        exchange.setMarkets(orders, transactions);

        let _realised_pnl = 0;                                                                                      // To be removed upon better arguments
        let _player_markets = {};                                                                                   // To be removed upon better arguments
        Object.keys(orders).forEach((market) => {                                                                   // To be removed upon better arguments
            _pos = 0;
            _cash = 0;
            transactions[market].forEach((transaction) => {
                if (transaction.buyerUser == player.getName()){
                    _pos += transaction.quantity;
                    _cash -= transaction.price * transaction.quantity;
                } if (transaction.sellerUser == player.getName()){
                    _pos -= transaction.quantity;
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
                _cash -= _pos * closingPrice;
                _pos = 0;
                _open_bids = 0;
                _open_asks = 0;
                _realised_pnl += _cash;
            }

            _player_markets[market] = {
                limit: positionLimit,
                pos: _pos,
                open_bids: _open_bids,
                open_asks: _open_asks,
                cash: _cash,
            }
        });

        let _player = {                                                                                             // To be removed upon better arguments
            player_markets: _player_markets,
            realised_pnl: _realised_pnl,
        }

        player.init(_player);

        console.log("ExchangeState", exchangeResponse, exchange);
        initProductView();
        initTransactionTable();
    });

    connection.on("StateUpdated", (newState) => {
        // exchange.state = newState;

        exchange.setState(newState);

        console.log("StateUpdated", exchange);
        updateModal()
    });

    // Called when client or another player joins lobby (I think)
    // Message should also include position limits for each market and whether each market is settled or unsettled
    connection.on("LobbyState", (message) => {

        exchange.init(message.markets, message.participants, message.exchangeName, message.exchangeCode, message.state);

        generateTabs(Object.keys(exchange.markets));

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

// check if position exceeds position limit
function positionExceeded(quantity, market) {

    return player.positionExceeded(quantity, market);
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

// Connect to the SignalR hub
const [connection, start] = bindConnection(jwt);
start()