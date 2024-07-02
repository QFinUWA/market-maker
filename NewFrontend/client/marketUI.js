import { OrderTable } from "./order_table";
import { OrderInput } from "./order_input";
import { ProductView } from "./product_view";
import { TransactionTable } from "./transaction_table";

export class MarketUI{
    #tab;
    #content;
    #orderTable;
    #orderInput;
    #productView;
    #transactionTable;

    constructor(market_name, tabId, contentId, player, connection, market){

        // Create tab element
        this.#tab = document.createElement('div');
        this.#tab.id = tabId;
        this.#tab.classList.add('tab', inactiveTabColor, 'hover:'+activeTabColor, 'px-4', 'rounded-t-md', 'cursor-pointer');
        this.#tab.textContent = market_name;

        // Create content element
        this.#content = document.createElement('div');
        this.#content.id = contentId;
        this.#content.classList.add('p-4', 'bg-gray-200', 'hidden', 'h-full', 'flex', 'rounded-br-lg', 'rounded-bl-lg', 'rounded-tr-lg');

        const leftSide = document.createElement('div');
        leftSide.classList.add('w-1/2', 'h-full', 'pl-2', "pr-4", "overflow-y-auto");

        const orderTable = document.createElement('table');
        orderTable.id = `orders-${market_name}`;
        this.#orderTable = new OrderTable(orderTable, player, connection, market, market_name);

        leftSide.appendChild(orderTable);

        const rightSide = document.createElement('div');
        rightSide.classList.add('w-1/2', 'h-full', "pr-2", "pl-4");

        const topRightSide = document.createElement('div');
        topRightSide.classList.add('w-full', 'h-1/2', "overflow-y-auto", "flex");

        const bottomRightSide = document.createElement('div');
        bottomRightSide.classList.add('w-full', 'h-1/2', "overflow-y-auto");

        const orderInputDiv = document.createElement('div');
        orderInputDiv.id = `orderInput-${market_name}`;
        this.#orderInput = new OrderInput(orderInputDiv, player, connection, market, market_name);

        const productViewDiv = document.createElement('div');
        productViewDiv.id = `productView-${market_name}`;
        this.#productView = new ProductView(productViewDiv, player, market_name);

        const transactionTable = document.createElement('table');
        transactionTable.id = `transactions-${market_name}`;
        this.#transactionTable = new TransactionTable(transactionTable, market);

        topRightSide.appendChild(orderInputDiv);
        topRightSide.appendChild(productViewDiv);
        bottomRightSide.appendChild(transactionTable);

        rightSide.appendChild(topRightSide);
        rightSide.appendChild(bottomRightSide);

        this.#content.appendChild(leftSide);
        this.#content.appendChild(rightSide);
    }

    getTab(){
        return this.#tab;
    }

    getContent(){
        return this.#content;
    }

    initOrderTable(){
        this.#orderTable.init();
    }

    initOrderInput(){
        this.#orderInput.init();
    }

    initProductView(){
        this.#productView.init();
    }

    initTransactionTable(){
        this.#transactionTable.init();
    }

    updateOrderTable(){
        this.#orderTable.updateContent();
    }

    updateProductView(){
        this.#productView.updateContent();
    }

    updateTransactionTable(){
        this.#transactionTable.updateContent();
    }
}