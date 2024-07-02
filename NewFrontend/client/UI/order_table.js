export class OrderTable{
    #table;
    #market;
    #player;
    #market_name;
    #connection;
    #body;

    constructor(table, player, connection, market, market_name){
        this.#table = table;
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
            console.log(`buy ${price}`);

        } else if(action == "a"){
            this.#connection
                .invoke("PlaceOrder", this.#market_name, price, quantity, "ref")
                .catch((err) => console.error(err.toString()));
            console.log(`sell ${price}`);
        }
    }

    deleteOrder(order){
        console.log("DeleteOrder", order);
        // cancel order
        connection
            .invoke("DeleteOrder", order.id)
            .catch((err) => console.error(err.toString()));
    }

    updateContent(){
        let minPrice = this.#market.getMinPrice();
        let maxPrice = this.#market.getMaxPrice();

        orders = this.#market.getSortedOrders();
        
        for (let order of orders) {
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
            row.addEventListener("dblclick", (event) => {
                // check if I am the person who made order
                if (document.getElementById("playerName").textContent != order.user){
                    let price = order.price;
                    let quantity = -order.quantity;

                    if (quantity < 0){
                        placeOrder('a', quantity, price);
                    }else{
                        placeOrder('b', quantity, price);
                    }
                } else {
                    deleteOrder(order);
                }
            });
        }
    }

    init(){
        this.#table.innerHTML = ''; // erase existing content
        this.#table.classList.add("w-full");
        let thead = this.#table.createTHead();
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

        this.#body = this.#table.createTBody();
    }
}