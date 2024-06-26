export class PlayerMarket{
    #open_bids = 0;
    #open_asks = 0; // This is positive when there are open asks
    #pos = 0;
    #limit = 99999;
    #cash = 0;

    init(player_market){
        this.#open_bids = player_market.open_bids;
        this.#open_asks = player_market.open_asks;
        this.#pos = player_market.pos;
        this.#limit = player_market.limit;
        this.#cash = player_market.cash;
    }

    closeMarket(closingPrice){
        this.#cash -= this.#pos * closingPrice;
        this.#pos = 0;
        this.#open_bids = 0;
        this.#open_asks = 0;
        return this.#cash; 
    }

    positionExceeded(quantity){
        return Math.abs(this.#pos + this.#open_bids + quantity) > this.#limit || Math.abs(this.#pos - this.#open_asks + quantity) > this.#limit;
    }
}