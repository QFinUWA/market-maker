export class Order{
    constructor(order){
        this.id = order.id;
        this.user = order.user;
        this.quantity = order.quantity;
        this.price = order.price;
        this.timeStamp = order.timeStamp;
    }
}