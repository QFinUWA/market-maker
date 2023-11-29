using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _exchange = new();
        private readonly List<string> _participants = new();
        private readonly MarketConfig _config = new();
        private readonly MarketState _state = MarketState.InLobby; 
        
        public MarketConfig Config { get;  }

        public MarketState State { get; set; }
        
        public List<string> Exchanges
        {
            get
            {
                return _exchange.Keys.ToList();
            } 
        }

        public List<string> Participants { get;  } 

        public List<Transaction> Transactions
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
        
        public List<Order> Orders
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
        public void AddParticipant(string username)
        {
            if (_participants.Contains(username)) return;
            
            _participants.Add(username);
        }

        // return error
        public void DeleteOrder(Guid id, string user)
        {
            foreach (var market in _exchange.Values)
            {
                if (market.DeleteOrder(id, user)) return;
            }
        }

        public (Order, List<Transaction>) NewOrder(string username, string exchange, int price, int quantity)
        {
            var order = Order.MakeOrder(
                username,
                exchange,
                price,
                quantity);

            var originalOrder = (Order)order.Clone();

            var transactions = _exchange[order.Exchange].NewOrder(order);

            return (originalOrder, transactions);
        }

        public void AddExchange(string market)
        {
            _exchange.Add(market, new Exchange());
        }

        public Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
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
