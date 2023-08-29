using MarketMakerAPI.Models;

namespace MarketMakerAPI.Services
{
    public class LocalMarketService : IMarketService
    {
        private static readonly Dictionary<int, List<Guid>> _market = new();
        private static readonly Dictionary<Guid, Order> _orders = new();

        public List<Order> Orders
        {
            get => _orders.Values.ToList();
        }


        public void DeleteOrder(Guid id)
        {
            Order order = _orders[id];

            _market[order.Price].Remove(order.Id);

            if (_market[order.Price].Count == 0)
            {
                _market.Remove(order.Price);
            }

            _orders.Remove(order.Id);
        }

        public void NewOrder(Order order)
        {

            _orders.Add(order.Id, order);
            if (!_market.ContainsKey(order.Price))
            {
                _market[order.Price] = new List<Guid>();
            }
            _market[order.Price].Add(order.Id);
        }

        public Order UpdateOrder(Order order)
        {
            Order oldOrder = _orders[order.Id];
            _orders[order.Id] = order;
            return oldOrder;
        }


    }
}
