export class ProductView{
    #div;
    #player;
    #position;
    #buys;
    #sells;
    #cash;
    #settlement;
    #market_name;

    constructor(div, player, market_name){
        this.#div = div;
        this.#player = player;
        this.#market_name = market_name;
    }

    updateContent(){
        let newInfo = this.#player.productInfo(this.#market_name);
        this.#position.innerHTML = newInfo.position;
        this.#buys.innerHTML = newInfo.buys;
        this.#sells.innerHTML = newInfo.sells;
        this.#cash.innerHTML = newInfo.cash;
        this.#settlement.innerHTML = newInfo.settlement;
    }

    init(){
        this.#div.innerHTML='';
        this.#div.classList.add("w-1/3", "min-h-[50%]", "gap-[2%]", "border", "border-black");

        let positionDiv = document.createElement("div");
        positionDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let position_text = document.createElement("p");
        // position_text.classList.add("h-[10%]", "w-[100%]", "text-center", "border", "border-black", "flex", "items-center", "justify-center");
        position_text.innerHTML = "POSITION";
        this.#position = document.createElement("p");
        // position.classList.add("h-[10%]", "w-[100%]", "text-center");
        this.#position.innerHTML = 0;
        this.#position.id = `position-${marketToTabID[marketKey]}`;

        let bnsDiv = document.createElement("div");
        bnsDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "items-center", "justify-center");

        let buysDiv = document.createElement("div");
        buysDiv.classList.add("h-[100%]", "w-[50%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let buys_text = document.createElement("p");
        buys_text.classList.add("w-[100%]", "text-center");
        buys_text.innerHTML = "BUYS";
        this.#buys = document.createElement("p");
        this.#buys.classList.add("w-[100%]", "text-center");
        this.#buys.innerHTML = 0;
        this.#buys.id = `buys-${marketToTabID[marketKey]}`;

        let sellsDiv = document.createElement("div");
        sellsDiv.classList.add("h-[100%]", "w-[50%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let sells_text = document.createElement("p");
        sells_text.classList.add("w-[100%]", "text-center");
        sells_text.innerHTML = "SELLS";
        this.#sells = document.createElement("p");
        this.#sells.classList.add("w-[100%]", "text-center");
        this.#sells.innerHTML = 0;
        this.#sells.id = `sells-${marketToTabID[marketKey]}`;

        let cashDiv = document.createElement("div");
        cashDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let cash_text = document.createElement("p");
        cash_text.classList.add("w-[100%]", "text-center");
        cash_text.innerHTML = "CASH";
        this.#cash = document.createElement("p");
        this.#cash.classList.add("w-[100%]", "text-center");
        this.#cash.innerHTML = 0;
        this.#cash.id = `cash-${marketToTabID[marketKey]}`;

        let settlementDiv = document.createElement("div");
        settlementDiv.classList.add("h-[25%]", "w-[100%]", "text-center", "border", "border-black", "flex", "flex-col", "items-center", "justify-center");
        let settlement_text = document.createElement("p");
        settlement_text.classList.add("w-[100%]", "text-center");
        settlement_text.innerHTML = "SETTLEMENT";
        this.#settlement = document.createElement("p");
        this.#settlement.classList.add("w-[100%]", "text-center");
        this.#settlement.innerHTML = "????";
        this.#settlement.id = `settlement-${marketToTabID[marketKey]}`;
        
        positionDiv.appendChild(position_text);
        positionDiv.appendChild(this.#position);
        d.appendChild(positionDiv);
        buysDiv.appendChild(buys_text);
        buysDiv.appendChild(this.#buys);
        sellsDiv.appendChild(sells_text);
        sellsDiv.appendChild(this.#sells);
        bnsDiv.appendChild(buysDiv);
        bnsDiv.appendChild(sellsDiv);
        d.appendChild(bnsDiv);
        cashDiv.appendChild(cash_text);
        cashDiv.appendChild(this.#cash);
        d.appendChild(cashDiv);
        settlementDiv.appendChild(settlement_text);
        settlementDiv.appendChild(this.#settlement);
        d.appendChild(settlementDiv);
    }
}