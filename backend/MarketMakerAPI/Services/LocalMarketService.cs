using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _market;

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
            
            foreach (Exchange exchange in _market.Values)
            {
                orders.AddRange(exchange.GetOrders());
            }

            return orders;
        }


        public void DeleteOrder(string market, Guid id)
        {
            _market[market].DeleteOrder(id);
        }

        public List<Order> NewOrder(Order order)
        {
            return _market[order.Exchange].NewOrder(order);
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

        //public Order UpdateOrder(Order order)
        //{
        //    Order oldOrder = _orders[order.Id];
        //    _orders[order.Id] = order;
        //    return oldOrder;
        //}


    }
}
