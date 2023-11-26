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

        public LocalMarketService()
        {
            // TEMPORARY
            //_market.Add("IYE", new Exchange());
            _market = new Dictionary<string, Exchange>();
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


        public void DeleteOrder(Guid id)
        {
            foreach (var market in _market.Values)
            {
                if (market.DeleteOrder(id)) return;
            }
        }

        public List<Order> NewOrder(Order order)
        {
            return _market[order.Exchange].NewOrder(order);
        }

        public void AddExchange(string market)
        {
            _market.Add(market, new Exchange());
        }

        //public Order UpdateOrder(Order order)
        //{
        //    Order oldOrder = _orders[order.Id];
        //    _orders[order.Id] = order;
        //    return oldOrder;
        //}


    }
}
