using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : IMarketService
    {
        private readonly Dictionary<string, Exchange> _market = new();

        public LocalMarketService()
        {
            // TEMPORARY
            _market.Add("IYE", new Exchange());
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
            return _market[order.Market].NewOrder(order);
        }

        //public Order UpdateOrder(Order order)
        //{
        //    Order oldOrder = _orders[order.Id];
        //    _orders[order.Id] = order;
        //    return oldOrder;
        //}


    }
}
