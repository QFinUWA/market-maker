using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _exchange = new();
        private readonly List<string> _participants = new();

        public IEnumerable<string> Exchanges
        {
            get
            {
                return _exchange.Keys;
            } 
        }

        public IEnumerable<string> Participants
        {
            get
            {
                return _participants;
            }
        }

        public List<Order> GetOrders()
        {

            List<Order> orders = new();
            
            foreach (var exchange in _exchange.Values)
            {
                orders.AddRange(exchange.GetOrders());
            }

            return orders;
        }

        // return error
        public void DeleteOrder(Guid id, string user)
        {
            foreach (var market in _exchange.Values)
            {
                if (market.DeleteOrder(id, user)) return;
            }
        }

        public (Order, List<TransactionEvent>) NewOrder(string username, string exchange, int price, int quantity)
        {
            var order = Order.MakeOrder(
                username,
                exchange,
                price,
                quantity);

            var originalOrder = (Order)order.Clone();

            List<TransactionEvent> transactions = _exchange[order.Exchange].NewOrder(order);

            return (originalOrder, transactions);
        }

        public void AddExchange(string market)
        {
            _exchange.Add(market, new Exchange());
        }

        public Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            Dictionary<string, float> profits = new();
            foreach (var exchangeKeyValue in _exchange)
            {
                var exchangeName = exchangeKeyValue.Key;
                var exchange = exchangeKeyValue.Value;

                var price = prices[exchangeName];
                
                exchange.Close(price);

                foreach (var userProfit in exchange.userProfits)
                {
                    profits[userProfit.Key] = profits.GetValueOrDefault(userProfit.Key, 0) + userProfit.Value;
                }
            }

            return profits;
        }




    }
}
