import { PlayerMarket } from "./player_market";

export class Player{
    #name;
    #markets = {};
    #realisedPnL = 0;

    constructor(name){
        this.#name = name;
    }

    init(player){
        Object.keys(player.player_markets).forEach((market) => {
            this.#markets[market].init(player.player_markets[market]);
        });
        this.#realisedPnL = player.realised_pnl;
    }

    closeMarkets(closingPrices){
        Object.keys(closingPrices).forEach((market) => {
            this.#realisedPnL += this.#markets[market].closeMarket(closingPrices[market]);
        })
    }

    positionExceeded(quantity, market){
        return this.#markets[market].positionExceeded(quantity);
    }

    getName(){
        return this.#name;
    }
}