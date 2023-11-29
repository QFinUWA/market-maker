using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _exchange = new();
        private readonly List<string> _participants = new();
        private readonly MarketConfig _config = new();
        private MarketState _state = MarketState.InLobby;
        // public int MyProperty { get; set; } = 42;
        public MarketConfig Config
        {
            get
            {
                return _config;
            }
        }

        public MarketState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }
        
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
            if (_state != MarketState.Open) throw new InvalidOperationException();
            
            foreach (var market in _exchange.Values)
            {
                if (market.DeleteOrder(id, user)) return;
            }
        }

        public (Order, List<Transaction>) NewOrder(string username, string exchange, int price, int quantity)
        {
            if (_state != MarketState.Open) throw new InvalidOperationException();
            
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
            if (_state != MarketState.InLobby) throw new InvalidOperationException();
            
            _exchange.Add(market, new Exchange());
        }

        public Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            if (_state == MarketState.InLobby) throw new InvalidOperationException();
            if (_state == MarketState.Closed) throw new InvalidOperationException();

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
