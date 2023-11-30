using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : MarketService
    {
        private readonly Dictionary<string, Exchange> _exchange = new();

        public override List<string> Exchanges
        {
            get
            {
                return _exchange.Keys.ToList();
            } 
        }

        public override List<string> Participants { get; } = new(); 

        public override List<Transaction> Transactions
        {
            get
            {
                List<Transaction> transactions = new();
            
                foreach (var exchange in _exchange.Values)
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
                
                foreach (var exchange in _exchange.Values)
                {
                    orders.AddRange(exchange.Orders);
                }

                return orders;
            }
        }
        public override bool AddParticipant(string username)
        {
            if (Participants.Contains(username)) return false;
            
            Participants.Add(username);
            return true;
        }

        public override bool DeleteOrder(Guid id, string user)
        {
            return _exchange.Values.Any(market => market.DeleteOrder(id, user));
        }

        public override List<Transaction>? NewOrder(Order newOrder)
        {
            if (!_exchange.ContainsKey(newOrder.Exchange)) return null;
            
            var transactions = _exchange[newOrder.Exchange].NewOrder(newOrder);
            return transactions;
        }

        public override bool AddExchange(string market)
        {
            if (_exchange.ContainsKey(market)) return false;
            _exchange.Add(market, new Exchange());
            return true;
        }

        public override Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            Dictionary<string, float> profits = new();
            foreach (var (exchangeName, exchange) in _exchange)
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
