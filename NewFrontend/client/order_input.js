export class OrderInput {
    #div;
    #player;
    #connection;
    #market;
    #market_name;

    constructor(div, player, connection, market, market_name){
        this.#div = div;
        this.#player = player;
        this.#connection = connection;
        this.#market = market;
        this.#market_name = market_name;
    }

    placeOrder(action, quantity, price){
        if (this.#player.positionExceeded(quantity, this.#market_name)) {
            // flash the position div red
            document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.add("!bg-red-500");
            setTimeout(() => {
                document.getElementById(`position-${marketToTabID[marketKey]}`).parentElement.classList.remove("!bg-red-500");
            }, 500);
        }
        else if(action == "b"){
            this.#connection
                .invoke("PlaceOrder", this.#market_name, price, quantity, "ref")
                .catch((err) => console.error(err.toString()));
            console.log(`bid ${price}`);

        } else if(action == "a"){
            this.#connection
                .invoke("PlaceOrder", this.#market_name, price, quantity, "ref")
                .catch((err) => console.error(err.toString()));
            console.log(`ask ${price}`);
        }
    }

    deleteOrderAtPrice(price){
        let name = this.#player.getName();
        let myOrders = this.#market.getFilteredOrders((o) => o.user == name && o.price == price);
        for(let myOrder of myOrders){
            this.#connection
                .invoke("DeleteOrder", myOrder.id)
                .catch((err) => console.error(err.toString()));
        }
    }

    deleteOrderAll(action){ // action {'c': cancel all, 'b': cancel bids, 'a': cancel asks}
        let name = this.#player.getName();
        switch(actions){
            case 'a':    
                let myAsks = this.#market.getFilteredOrders((o) => o.user == name && o.quantity < 0);
                for(let myAsk of myAsks){
                    this.#connection
                        .invoke("DeleteOrder", myAsk.id)
                        .catch((err) => console.error(err.toString()));
                }
                console.log("Cancel Asks");
                break;
            case 'b':
                let myBids = this.#market.getFilteredOrders((o) => o.user == name && o.quantity > 0);
                for(let myBid of myBids){
                    this.#connection
                        .invoke("DeleteOrder", myBid.id)
                        .catch((err) => console.error(err.toString()));
                }
                console.log("Cancel Bids");
                break;
            case 'c':
                let myOrders = this.#market.getFilteredOrders((o) => o.user == name);
                for(let myOrder of myOrders){
                    this.#connection
                        .invoke("DeleteOrder", myOrder.id)
                        .catch((err) => console.error(err.toString()));
                }
                console.log("Cancel All");
                break;
        }
    }

    init(){
        this.#div.innerHTML='';
        this.#div.classList.add("w-2/3", "min-h-[50%]", "gap-[2%]", "border", "border-black");

        quickInput = document.createElement("input");
        quickInput.classList.add("h-[33%]", "w-[100%]", "border", "border-black", "text-center", "text-lg");
        quickInput.placeholder = "Quick Input";
        quickInput.addEventListener("keydown", (event) => {
            if(event.key === "Enter"){
                if(quickInput.value.match(/^[0-9]*[ba][0-9]+$/) || quickInput.value.match(/^[c][0-9]+$/)){
                    // if quantity number exists use it otherwise set to 1
                    let action = quickInput.value.match(/[abc]/)[0];
                    let price = parseInt(quickInput.value.match(/[0-9]+$/)[0]);
                    if (action == "c") {
                        // cancel all orders at that price
                        deleteOrderAtPrice(price);
                    }
                    else {
                        let quantity = quickInput.value.match(/^[0-9]+/) ? parseInt(quickInput.value.match(/^[0-9]+/)[0]) : 1;
                        quantity = action == "b" ? quantity : -quantity;
                        if(!isNaN(price)){
                            placeOrder(action, quantity, price);
                        }
                    }
                }   
                quickInput.value = "";
            }
        });
        quickInput.addEventListener("input", (event) => {
            // remove invalid characters
            quickInput.value = quickInput.value.replace(/[^0-9abc]/g, "");
            // can only have one a, b, or c at a time
            if(quickInput.value.match(/[abc]/g) && quickInput.value.match(/[abc]/g).length > 1){
                quickInput.value = quickInput.value.slice(0, -1);
            }
            // if it matches regex set font medium
            if(quickInput.value.match(/^[0-9]*[ba][0-9]+$/) || quickInput.value.match(/^[c][0-9]+$/)) {
                quickInput.classList.add("!font-medium");
            } else {
                quickInput.classList.remove("!font-medium");
            }
        });

        inputDiv = document.createElement("div");
        inputDiv.classList.add("h-[33%]", "flex");

        bidDiv = document.createElement("div");
        bidDiv.classList.add("h-[100%]", "w-[50%]", "border", "border-black");

        bidPriceInput = document.createElement("input");
        bidPriceInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        bidPriceInput.type = "number";
        bidPriceInput.min = "0";
        bidPriceInput.step = "1";
        bidPriceInput.placeholder = "Price";
        bidPriceInput.id = `bidPriceInput-${marketToTabID[marketKey]}`;

        bidQuantityInput = document.createElement("input");
        bidQuantityInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        bidQuantityInput.type = "number";
        bidQuantityInput.min = "0";
        bidQuantityInput.step = "1";
        bidQuantityInput.placeholder = "Quantity";
        bidQuantityInput.id = `bidQuantityInput-${marketToTabID[marketKey]}`;
        
        bidButtonDiv = document.createElement("div");
        bidButtonDiv.classList.add("h-[50%]", "w-[100%]", "flex", "justify-center", "items-center");

        bidButton = document.createElement("button");
        bidButton.classList.add("h-[70%]", "w-[70%]", "text-center", "text-sm", "font-medium", "bg-green-600", "hover:bg-green-700", "text-white", "rounded-md");
        bidButton.innerHTML = "Send Bid";
        bidButton.id = `bidButton-${marketToTabID[marketKey]}`;
        bidButton.addEventListener("click", (event) => {
            let price = bidPriceInput.value;
            let quantity = bidQuantityInput.value;
            placeOrder('b', quantity, price);
        });

        askDiv = document.createElement("div");
        askDiv.classList.add("h-[100%]", "w-[50%]", "border", "border-black");

        askPriceInput = document.createElement("input");
        askPriceInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        askPriceInput.type = "number";
        askPriceInput.min = "0";
        askPriceInput.step = "1";
        askPriceInput.placeholder = "Price";
        askPriceInput.id = `askPriceInput-${marketToTabID[marketKey]}`;

        askQuantityInput = document.createElement("input");
        askQuantityInput.classList.add("h-[50%]", "w-[50%]", "text-center", "text-md");
        askQuantityInput.type = "number";
        askQuantityInput.min = "0";
        askQuantityInput.step = "1";
        askQuantityInput.placeholder = "Quantity";
        askQuantityInput.id = `askQuantityInput-${marketToTabID[marketKey]}`;

        askButtonDiv = document.createElement("div");
        askButtonDiv.classList.add("h-[50%]", "w-[100%]", "flex", "justify-center", "items-center");

        askButton = document.createElement("button");
        askButton.classList.add("h-[70%]", "w-[70%]", "text-center", "text-sm", "font-medium", "bg-red-600", "hover:bg-red-800", "text-white", "rounded-md");
        askButton.innerHTML = "Send Ask";
        askButton.id = `askButton-${marketToTabID[marketKey]}`;
        askButton.addEventListener("click", (event) => {
            let price = askPriceInput.value;
            let quantity = askQuantityInput.value;
            placeOrder('a', -quantity, price);
        });

        cancelBids = document.createElement("button");
        cancelBids.classList.add("h-[17%]", "w-[50%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelBids.innerHTML = "Cancel Bids";
        cancelBids.addEventListener("click", (event) => {
            deleteOrderAll('b');
        });

        cancelAsks = document.createElement("button");
        cancelAsks.classList.add("h-[17%]", "w-[50%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelAsks.innerHTML = "Cancel Asks";
        cancelAsks.addEventListener("click", (event) => {
            deleteOrderAll('a');
        });

        cancelAll = document.createElement("button");
        cancelAll.classList.add("h-[17%]", "w-[100%]", "border", "border-black", "text-sm", "bg-gray-300", "hover:bg-gray-600");
        cancelAll.innerHTML = "Cancel All";
        cancelAll.addEventListener("click", (event) => {
            deleteOrderAll('c');
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
        this.#div.appendChild(quickInput);
        this.#div.appendChild(inputDiv);
        this.#div.appendChild(cancelBids);
        this.#div.appendChild(cancelAsks);
        this.#div.appendChild(cancelAll);
    }
}