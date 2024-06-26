import { Market } from "./market";

export class Exchange {
    #markets = {};
    #participants = [];
    #state; // 'Lobby', 'Open', 'Paused', 'Closed'
    #exchangeName;
    #exchangeCode;

    init(markets, participants, exchangeName, exchangeCode, state){
        this.#participants = participants;
        this.#exchangeName = exchangeName;
        this.#exchangeCode = exchangeCode;
        this.#state = state;

        this.#markets = {};
        markets.forEach((market) => { // Currently each market is [marketCode, string(Market object)] so the second element is basically unusable
            if (!(market[0] in this.#markets))  {
                this.#markets[market[0]] = new Market(false);
            }
        });
    }

    // Currently this is done in a round-about way, it will be better once orders and transactions are sent separated by market
    setMarkets(orders, transactions){
        Object.keys(orders.#markets).forEach((market) => {
            this.#markets[market].setOrders(orders[market]);
            this.#markets[market].setTransactions(transactions[market]);
        });
    }

    setState(state){
        this.#state = state;
    }

    newOrder(market, order, transactions){
        if (order.quantity != 0) {
            this.#markets[market].addOrder(order);
        }

        for (const transaction of transactions) {
            this.#markets[market].transactions.push(transaction);
        }
    }

    // This is used to find the market an orderID belongs to when deleting, this will be removed once backend send deleteOrder info with the market as an argument
    getMarkets(){
        return this.#markets;
    }

    // This is used to find the market an orderID belongs to when deleting, this will be removed once backend send deleteOrder info with the market as an argument
    getMarketOrders(market){
        return this.#markets[market].getMarketOrders();
    }

    deleteOrder(market, orderID){
        this.#markets[market].deleteOrder(orderID);
    }

    newParticipant(user){
        this.#participants.push(user);
    }
    
    closeMarkets(markets){
        markets.forEach((market) => {
            this.#markets[market].close();
        })
    }

    isClosed(market){
        return this.#markets[market].isClosed();
    }
};