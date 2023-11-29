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
        public override void AddParticipant(string username)
        {
            if (Participants.Contains(username)) return;
            
            Participants.Add(username);
        }

        // return error
        public override void DeleteOrder(Guid id, string user)
        {
            if (State != MarketState.Open) throw new InvalidOperationException();
            
            foreach (var market in _exchange.Values)
            {
                if (market.DeleteOrder(id, user)) return;
            }
        }

        public override (Order, List<Transaction>) NewOrder(string username, string exchange, int price, int quantity)
        {
            if (State != MarketState.Open) throw new InvalidOperationException();
            
            var order = new Order(
                username,
                exchange,
                price,
                quantity);

            var originalOrder = (Order)order.Clone();

            var transactions = _exchange[order.Exchange].NewOrder(order);

            return (originalOrder, transactions);
        }

        public override void AddExchange(string market)
        {
            if (State != MarketState.Lobby) throw new InvalidOperationException();
            
            _exchange.Add(market, new Exchange());
        }

        public override Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            if (State == MarketState.Lobby) throw new InvalidOperationException();
            if (State == MarketState.Closed) throw new InvalidOperationException();

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
