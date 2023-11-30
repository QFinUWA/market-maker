using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : MarketService
    {
        protected readonly Dictionary<string, Exchange> Exchange = new();

        public override List<string> Exchanges
        {
            get
            {
                return Exchange.Keys.ToList();
            } 
        }

        public override List<Transaction> Transactions
        {
            get
            {
                List<Transaction> transactions = new();
            
                foreach (var exchange in Exchange.Values)
                {
                    transactions.AddRange(exchange.Transactions);
                }
                
                return transactions;
            } 
        }
        
        public override List<Order> Orders
        {
            get
            {
                List<Order> orders = new();
                
                foreach (var exchange in Exchange.Values)
                {
                    orders.AddRange(exchange.Orders);
                }

                return orders;
            }
        }

        public override bool DeleteOrder(Guid id, string user)
        {
            return Exchange.Values.Any(market => market.DeleteOrder(id, user));
        }

        public override List<Transaction>? NewOrder(Order newOrder)
        {
            if (!Exchange.ContainsKey(newOrder.Exchange)) return null;
            
            var transactions = Exchange[newOrder.Exchange].NewOrder(newOrder);
            return transactions;
        }

        public override bool AddExchange(string exchange)
        {
            if (Exchange.ContainsKey(exchange)) return false;
            Exchange.Add(exchange, new Exchange());
            return true;
        }

        public override Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            Dictionary<string, float> profits = new();
            foreach (var (exchangeName, exchange) in Exchange)
            {
                var price = prices[exchangeName];
                
                exchange.Close(price);

                foreach (var userProfit in exchange.UserProfits)
                {
                    profits[userProfit.Key] = profits.GetValueOrDefault(userProfit.Key, 0) + userProfit.Value;
                }
            }

            return profits;
        }

    }
}
