using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class Exchange
    {
        private readonly Dictionary<Guid, Order> _orders = new();

        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> Bid = new();
        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> Ask = new();

        public readonly Dictionary<string, float> UserProfits = new();
        public readonly List<Transaction> Transactions = new();

        public Order GetOrder(Guid id)
        {
            return _orders[id];
        }

        public IEnumerable<Order> Orders => _orders.Values;


        public List<Transaction> NewOrder(Order order) 
        {
            _orders.Add(order.Id, order);

            UserProfits.TryAdd(order.User, 0);
            
            // TODO: maybe set Order price to the lowestAsk if it is above it etc ...

            bool sideIsBid = order.Quantity > 0;

            var side = sideIsBid ? Bid : Ask;
            var otherSide = !sideIsBid ? Bid : Ask;

            int price = order.Price;

            side.TryAdd(price, new PriorityQueue<Guid, DateTime>());

            // assuming market was balanced before, only check for new updates
            // if this is the newest order
            List<Transaction> transactions = new();

            side[price].Enqueue(order.Id, order.TimeStamp);

            if (side[price].Count > 1 || !otherSide.ContainsKey(price))
            {
                return transactions;
            }
            
            // keep removing from queue until first order exists
            while (otherSide[price].Count > 0 && order.Quantity != 0)
            {
                DateTime now = DateTime.Now;
                Guid otherId = otherSide[price].Peek();

                // dormant deleted orders
                if (!_orders.ContainsKey(otherId))
                {
                    otherSide[price].Dequeue();
                    continue;
                }
                
                Order otherOrder = _orders[otherId];
                int quantityTraded; 
                
                if (Math.Sign(order.Quantity + otherOrder.Quantity) != Math.Sign(order.Quantity))
                {
                    quantityTraded = order.Quantity;
                    
                    otherOrder.Quantity += order.Quantity;
                    UserProfits[otherOrder.User] += order.Quantity * price; 
                    order.Quantity = 0;
                    UserProfits[order.User] += -1 * order.Quantity * price;

                }
                else
                {
                    quantityTraded = otherOrder.Quantity;
                    
                    order.Quantity += otherOrder.Quantity;
                    UserProfits[order.User] += otherOrder.Quantity * price;
                    otherOrder.Quantity = 0;
                    UserProfits[otherOrder.User] += -1 * otherOrder.Quantity * price;
                }

                if (otherOrder.Quantity == 0)
                {
                    otherSide[price].Dequeue();
                    _orders.Remove(otherId);
                }

                var (buyer, seller) = sideIsBid ? (order, otherOrder) : (otherOrder, order);
                
                transactions.Add(new Transaction(
                        buyer.User,
                        buyer.Id,
                        seller.User,
                        seller.Id,
                        order.Exchange,
                        order.Price,
                        Math.Abs(quantityTraded),
                        order.User,
                        now
                    )
                );
            }
            
            if (order.Quantity == 0)
            {
                Guid removeId = side[price].Dequeue();
                _orders.Remove(removeId);
            }
            Transactions.AddRange(transactions); 
            return transactions;
        }

        public bool DeleteOrder(Guid id, string user)
        {
            if (!_orders.ContainsKey(id)) return false;
            
            if (_orders[id].User != user) return false;
            
            _orders.Remove(id);

            return true;
        }

        public void Close(int price)
        {
            foreach (var order in _orders.Values)
            {
                UserProfits[order.User] += (price - order.Price) * order.Quantity;
            }
            
            _orders.Clear();
            Bid.Clear();
            Ask.Clear();
        }

        public void RemoveEmptyOrders()
        {
            Bid.Clear();
            Ask.Clear();
            
            foreach (var order in _orders.Values)
            {
                var side = order.Quantity > 0 ? Bid : Ask;
                side.TryAdd(order.Price, new PriorityQueue<Guid, DateTime>());
                side[order.Price].Enqueue(order.Id, order.TimeStamp);
            }
        }
    }
    
}
