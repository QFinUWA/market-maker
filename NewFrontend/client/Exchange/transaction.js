export class Transaction{
    constructor(transaction){
        this.buyerUser = transaction.buyerUser;
        this.buyerOrderId = transaction.buyerOrderId;
        this.sellerUser = transaction.sellerUser;
        this.sellerOrderId = transaction.sellerOrderId;
        this.price = transaction.price;
        this.quantity = transaction.quantity;
        if(transaction.passiveOrder = transaction.buyerOrderId){
            this.passiveUser = this.buyerUser;
            this.aggressiveUser = this.sellerUser;
        }else{
            this.passiveUser = this.sellerUser;
            this.aggressiveUser = this.buyerUser;
        }
        this.timeStamp = transaction.timeStamp;
    }
}