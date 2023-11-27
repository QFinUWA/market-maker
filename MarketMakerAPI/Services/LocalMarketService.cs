using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _market = new();

        public List<string> Exchanges
        {
            get
            {
                return _market.Keys.ToList();
            } 
        }

        public List<Order> GetOrders()
        {

            List<Order> orders = new();
            
            foreach (var exchange in _market.Values)
            {
                orders.AddRange(exchange.GetOrders());
            }

            return orders;
        }


        public void DeleteOrder(Guid id)
        {
            foreach (var market in _market.Values)
            {
                if (market.DeleteOrder(id)) return;
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

            List<TransactionEvent> transactions = _market[order.Exchange].NewOrder(order);

            return (originalOrder, transactions);
        }

        public void AddExchange(string market)
        {
            _market.Add(market, new Exchange());
        }

        public Dictionary<string, float> CloseMarket(Dictionary<string, int> prices)
        {
            Dictionary<string, float> profits = new();
            foreach (var exchangeKeyValue in _market)
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
