export class PlayerMarket{
    #open_bids = 0;
    #open_asks = 0; // This is positive when there are open asks
    #buys = 0;
    #sells = 0;
    #limit = 99999;
    #cash = 0;
    #closed = false;

    init(player_market){
        this.#open_bids = player_market.open_bids;
        this.#open_asks = player_market.open_asks;
        this.#buys = player_market.buys;
        this.#sells = player_market.sells;
        this.#limit = player_market.limit;
        this.#cash = player_market.cash;
    }

    getPos(){
        return this.#buys - this.#sells;
    }

    closeMarket(closingPrice){
        this.#cash -= this.getPos() * closingPrice;
        this.#buys = Math.max(this.#buys, this.#sells);
        this.#sells = Math.max(this.#buys, this.#sells);
        this.#open_bids = 0;
        this.#open_asks = 0;
        this.#closed = true;
        return this.#cash; 
    }

    positionExceeded(quantity){
        return Math.abs(this.getPos() + this.#open_bids + quantity) > this.#limit || Math.abs(this.getPos() - this.#open_asks + quantity) > this.#limit;
    }

    productInfo(){
        return {
            position: this.getPos(),
            buys: this.#buys,
            sells: this.#sells,
            cash: this.#cash,
            settlement: this.#closed ? this.#cash : '????'
        }
    }
}