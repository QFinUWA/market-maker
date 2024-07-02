import { Order } from "./order";
import { Transaction } from "./transaction";

export class Market{
    #orders = [];
    #transactions = [];
    #settled = false;

    constructor(settled){
        this.#settled = false;
    }

    clear(){
        this.#orders = [];
        this.#transactions = [];
    }

    setOrders(orders){
        this.#orders = [];
        orders.forEach((order) => {this.#orders.push(new Order(order))});
    }

    setTransactions(transactions){
        this.#transactions = [];
        transactions.forEach((transaction) => {this.#transactions.push(new Transaction(transaction))});
    }

    addOrder(order){
        this.#orders.push(new Order(order));
    }

    addTransaction(transaction){
        let passiveOrder = this.#orders.find((o) => o.id == transaction.passiveOrder);
        console.log("passiveOrder", passiveOrder);
        console.log("transaction", transaction);
        passiveOrder.quantity += transaction.quantity * (passiveOrder.quantity < 0 ? 1 : -1);
        if (passiveOrder.quantity == 0) {
            this.#orders = this.#orders.filter((o) => o.id != passiveOrder.id);
        }
    }

    // This is used to find the market an orderID belongs to when deleting, this will be removed once backend send deleteOrder info with the market as an argument
    getOrders(){
        return this.#orders;
    }

    deleteOrder(orderID){
        this.#orders = this.#orders.filter((o) => o.id != orderID);
    }

    close(){
        this.#settled = true;
    }

    isClosed(){
        return this.#settled;
    }
    
    getFilteredOrders(filter){
        return this.#orders.filter(filter);
    }

    getFilteredTransactions(filter){
        return this.#transactions.filter(filter);
    }

    getMinPrice(){
        return Math.min(...this.#orders.map((o) => o.price));
    }

    getMaxPrice(){
        return Math.max(...this.#orders.map((o) => o.price));
    }

    getSorterOrders(){
        this.#orders.sort((a, b) => b.price - a.price);
        return this.#orders;
    }
}