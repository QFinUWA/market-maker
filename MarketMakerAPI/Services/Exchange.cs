using MarketMaker.Models;
using System.Collections.Generic;

namespace MarketMaker.Services
{
    public class Exchange
    {
        private readonly Dictionary<Guid, Order> Orders = new();

        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> bid = new();
        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> ask = new();

        private readonly int highestBid = int.MinValue;
        private readonly int lowestAsk = int.MaxValue;

        public Dictionary<string, float> userProfits = new();

        public Order GetOrder(Guid id)
        {
            return Orders[id];
        }

        public List<Order> GetOrders()
        {
            return Orders.Values.ToList();
        }

        public List<TransactionEvent> NewOrder(Order order) 
        {
            Orders.Add(order.Id, order);

            userProfits.TryAdd(order.User, 0);
            
            // TODO: maybe set Order price to the lowestAsk if it is above it etc ...

            bool sideIsBid = order.Quantity > 0;

            var side = sideIsBid ? bid : ask;
            var otherSide = !sideIsBid ? bid : ask;

            int price = order.Price;

            side.TryAdd(price, new PriorityQueue<Guid, DateTime>());

            // assuming market was balanced before, only check for new updates
            // if this is the newest order
            List<TransactionEvent> transactions = new();

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
                if (!Orders.ContainsKey(otherId))
                {
                    otherSide[price].Dequeue();
                    continue;
                }
                
                Order otherOrder = Orders[otherId];
                int quantityTraded; 
                
                if (Math.Sign(order.Quantity + otherOrder.Quantity) != Math.Sign(order.Quantity))
                {
                    quantityTraded = order.Quantity;
                    
                    otherOrder.Quantity += order.Quantity;
                    userProfits[otherOrder.User] += order.Quantity * price; 
                    order.Quantity = 0;
                    userProfits[order.User] += -1 * order.Quantity * price;

                }
                else
                {
                    quantityTraded = otherOrder.Quantity;
                    
                    order.Quantity += otherOrder.Quantity;
                    userProfits[order.User] += otherOrder.Quantity * price;
                    otherOrder.Quantity = 0;
                    userProfits[otherOrder.User] += -1 * otherOrder.Quantity * price;
                }

                if (otherOrder.Quantity == 0)
                {
                    otherSide[price].Dequeue();
                    Orders.Remove(otherId);
                }

                transactions.Add(new TransactionEvent(
                    now, 
                    order.Id, 
                    otherOrder.Id,
                    Math.Abs(quantityTraded)));
            }
            
            if (order.Quantity == 0)
            {
                Guid removeId = side[price].Dequeue();
                Orders.Remove(removeId);
            }
            
            return transactions;
        }

        public bool DeleteOrder(Guid id)
        {
            if (!Orders.ContainsKey(id)) return false;
            
            Orders.Remove(id);

            return true;
        }

        public void Close(int price)
        {
            foreach (var order in Orders.Values)
            {
                userProfits[order.User] += (price - order.Price) * order.Quantity;
            }
            
            Orders.Clear();
            bid.Clear();
            ask.Clear();
        }
    }
    
}
