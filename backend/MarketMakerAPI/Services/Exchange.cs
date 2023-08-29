using MarketMakerAPI.Models;
using System.Collections.Generic;

namespace MarketMaker.Services
{
    public class Exchange
    {
        public Dictionary<Guid, Order> Orders = new();

        private readonly Dictionary<int, PriorityQueue<Guid, DateTime>> _bid = new();
        private readonly Dictionary<int, PriorityQueue<Guid, DateTime>> _ask = new();

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

            bool sideIsBid = order.Quantity > 0;

            var side = sideIsBid ? _bid : _ask;
            var otherSide = !sideIsBid ? _bid : _ask;

            if (!side.ContainsKey(order.Price)) side.Add(order.Price, new PriorityQueue<Guid, DateTime>());


            side[order.Price].Enqueue(order.Id, order.CreatedAt);

            // assuming market was balanced before, only check for new updates
            // if this is the newest order
            List<Order> ordersFilled = new();
            if (side[order.Price].Count > 1 || !otherSide.ContainsKey(order.Price)) return ordersFilled;
         
            // keep removing from queue until first order exists
            //while (side[order.Price].TryDequeue(out Guid id, out DateTime time))
            //{
            //    if (Orders.ContainsKey(id)) break;
            //}
            while (side[order.Price].Count > 0 && order.Quantity != 0)
            {
                Guid id = otherSide[order.Price].Peek();

                // dormant deleted orders
                if (!Orders.ContainsKey(id))
                {
                    otherSide[order.Price].Dequeue();
                    continue;
                }

                Order otherOrder = Orders[id];

                if (Math.Sign(order.Quantity + otherOrder.Quantity) != Math.Sign(order.Quantity))
                {
                    otherOrder.Quantity += order.Quantity;
                    order.Quantity = 0;

                    ordersFilled.Add(order);
                    Orders.Remove(id);
                }
                else
                {
                    order.Quantity += otherOrder.Quantity;
                    otherOrder.Quantity = 0;

                    Orders.Remove(otherOrder.Id);
                }
                
                ordersFilled.Add(otherOrder);
            }

            MakeTransactions();

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
