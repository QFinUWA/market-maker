import { MarketUI } from "./marketUI";

export class Navigator{
    #tabNavigation;
    #tabContent;
    #activeTabColor = "bg-gray-200";
    #inactiveTabColor = "bg-gray-100";
    #marketDisplays = {};

    constructor(tabNavigation, tabContent){
        this.#tabNavigation = tabNavigation;
        this.#tabContent = tabContent;
    }

    generateTabs(tabNames, player, connection, exchange){
        // Clear existing tabs and content
        this.#tabNavigation.innerHTML = '';
        this.#tabContent.innerHTML = '';

        // Create tabs and content dynamically
        tabNames.forEach((market_name) => {
            let tabId = `tab-${market_name}`;
            let contentId = `content-${market_name}`;

            this.#marketDisplays[market_name] = new MarketUI(market_name, tabId, contentId, player, connection, exchange.getName(market_name));
            newTab = this.#marketDisplays[market_name].getTab();
            newContent = this.#marketDisplays[market_name].getContent();
            tabNavigation.appendChild(newTab);
            tabContent.appendChild(newContent);
            
            // Click handler for tabs
            newTab.addEventListener('click', () => {
                const allContent = document.querySelectorAll('[id^="content-"]');
                allContent.forEach(item => item.classList.add('hidden'));
                document.getElementById(contentId).classList.remove('hidden');
                const allTabs = document.querySelectorAll('[id^="tab-"]');
                allTabs.forEach(item => item.classList.remove(activeTabColor));
                allTabs.forEach(item => item.classList.add(inactiveTabColor));
                document.getElementById(tabId).classList.remove(inactiveTabColor);
                document.getElementById(tabId).classList.add(activeTabColor);
            });
        });

        // Show the first tab initially
        tabNavigation.firstChild.click();
    }

    initOrderTables(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].initOrderTable();
        });
    }

    initOrderInputs(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].initOrderInput();
        });
    }

    initProductViews(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].initProductView();
        });
    }

    initTransactionTables(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].initTransactionTable();
        });
    }

    updateOrderTables(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].updateOrderTables();
        });
    }

    updateProductViews(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].updateProductView();
        });
    }

    updateTransactionTables(){
        Object.keys(this.#marketDisplays).forEach((market) => {
            this.#marketDisplays[market].updateTransactionTable();
        });
    }
}