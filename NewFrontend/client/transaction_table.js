export class TransactionTable{
    #table;
    #body;
    #market;
    #transaction_cols = [];
    #filter_ranges = {};

    constructor(table, market){
        this.#table = table;
        this.#market = market;
        this.#transaction_cols = ["Price", "Quantity", "Buyer", "Seller", "Aggressive", "Passive"];
        this.#filter_ranges = {"Selector": ""};
        for(let col of this.#transaction_cols){
            filter_ranges[col] = ["", ""];
        }
    }

    updateContent(){
        transactions = this.#market.getFilteredTransactions((t) => {
            transaction_dict = {
                "Price": t.price,
                "Quantity": t.quantity,
                "Buyer": t.buyerUser,
                "Seller": t.sellerUser,
                "Aggressive": t.aggressiveUser,
                "Passive": t.passiveUser
            }
            Object.keys(this.#transaction_cols).forEach((col) => {
                if(this.#filter_ranges[col][0] != "" && this.#filter_ranges[col][1] != ""){
                    if(this.#filter_ranges[col][0] == "=" && !transaction_dict[col].toString().includes(filter_ranges[col][1])){
                        return false;
                    }
                    if(filter_ranges[col][0] == "≥" && transaction_dict[col] < filter_ranges[col][1]){
                        return false;
                    }
                    if(filter_ranges[col][0] == "≤" && transaction_dict[col] > filter_ranges[col][1]){
                        return false;
                    }
                }
                if(["Buyer", "Seller", "Aggressive", "Passive"].includes(col) && this.#filter_ranges["Selector"] != ""){
                    if(["Buyer", "Seller", "Aggressive", "Passive"].every((col) => transaction_dict[col] != filter_ranges["Selector"])){
                        return false;
                    }
                }
            });
            return true;
        });

        for(let t of transactions){
            transaction_dict = {
                "Price": t.price,
                "Quantity": t.quantity,
                "Buyer": t.buyerUser,
                "Seller": t.sellerUser,
                "Aggressive": t.aggressiveUser,
                "Passive": t.passiveUser
            }
            let row = document.createElement("tr");
            row.classList.add("border-b", "border-gray-400", "[&>*]:px-2");
            Object.keys(transaction_dict).forEach((col) => {
                let cell = row.insertCell();
                if (["Buyer", "Seller", "Aggressive", "Passive"].includes(col)){
                    cell.addEventListener("click", (event) => {
                        if (this.#filter_ranges["Selector"] == event.target.textContent){
                            this.#filter_ranges["Selector"] = "";
                        } else {
                            this.#filter_ranges["Selector"] = event.target.textContent;
                        }
                        console.log(this.#filter_ranges);
                        updateContent();
                    });
                    cell.classList.add("cursor-pointer", "hover:bg-gray-300");
                    if (transaction_dict[col] == this.#filter_ranges["Selector"]){
                        cell.classList.add("bg-gray-300");
                    }
                }
                cell.textContent = transaction_dict[col];
            });
            this.#body.appendChild(row);
        }        
    }

    init(){
        this.#table.innerHTML = ''; // erase existing content
        this.#table.classList.add("w-full");
        let thead = this.#table.createTHead();
        thead.classList.add("sticky", "top-0");
        let header = thead.insertRow();
        
        for (let col of this.#transaction_cols) {
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

        let filters = thead.insertRow();
        filters.classList.add("bg-gray-200");
        for (let col of this.#transaction_cols) {
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
            select.addEventListener("change", (event) => {
                this.#filter_ranges[col][0] = event.target.value;
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
            input.addEventListener("input", (event) => {
                if(col == "Price" || col == "Quantity"){
                    this.#filter_ranges[col][1] = Math.round(parseFloat(event.target.value));
                }else{
                    this.#filter_ranges[col][0] = "="
                    this.#filter_ranges[col][1] = event.target.value;
                }
                updateContent();
            });

            cell.appendChild(input);
        }
    }
}