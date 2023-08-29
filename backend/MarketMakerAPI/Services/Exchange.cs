using MarketMaker.Models;
using System.Collections.Generic;

namespace MarketMaker.Services
{
    public class Exchange
    {
        public Dictionary<Guid, Order> Orders = new();

        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> _bid = new();
        public readonly Dictionary<int, PriorityQueue<Guid, DateTime>> _ask = new();

        private readonly int highestBid = int.MinValue;
        private readonly int lowestAsk = int.MaxValue;

        public Order GetOrder(Guid id)
        {
            return Orders[id];
        }
        public List<Order> GetOrders()
        {
            return Orders.Values.ToList();
        }

        public List<Order> NewOrder(Order order)
        {
            Orders.Add(order.Id, order);

            // TODO: maybe set Order price to the lowestAsk if it is above it etc ...

            bool sideIsBid = order.Quantity > 0;

            var side = sideIsBid ? _bid : _ask;
            var otherSide = !sideIsBid ? _bid : _ask;

            int price = order.Price;

            if (!side.ContainsKey(price)) side.Add(price, new PriorityQueue<Guid, DateTime>());

            // assuming market was balanced before, only check for new updates
            // if this is the newest order
            List<Order> ordersFilled = new();

            side[price].Enqueue(order.Id, order.CreatedAt);

            if (side[price].Count > 1 || !otherSide.ContainsKey(price))
            {
                return ordersFilled;
            }
            
            // keep removing from queue until first order exists
            while (otherSide[price].Count > 0 && order.Quantity != 0)
            {
                Guid otherId = otherSide[price].Peek();

                // dormant deleted orders
                if (!Orders.ContainsKey(otherId))
                {
                    otherSide[price].Dequeue();
                    continue;
                }

                Order otherOrder = Orders[otherId];

                if (Math.Sign(order.Quantity + otherOrder.Quantity) != Math.Sign(order.Quantity))
                {
                    otherOrder.Quantity += order.Quantity;
                    order.Quantity = 0;
                }
                else
                {
                    order.Quantity += otherOrder.Quantity;
                    otherOrder.Quantity = 0;
                }

                if (otherOrder.Quantity == 0)
                {
                    otherSide[price].Dequeue();
                    Orders.Remove(otherId);
                }

                 ordersFilled.Add(otherOrder);
            }
            
            if (order.Quantity == 0)
            {
                Guid removeId = side[price].Dequeue();
                Orders.Remove(removeId);

            }
            ordersFilled.Add(order);


            return ordersFilled;

        }

        public void DeleteOrder(Guid id)
        {
            Orders.Remove(id);
        }

        private void MakeTransactions()
        {
            // TODO: hit the most competitive bid/ask


        }
    }
}
