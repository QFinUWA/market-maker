using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalMarketService : MarketService
    {
        protected readonly Dictionary<string, Exchange> Exchange = new();

        public override List<Transaction> Transactions
        {
            get
            {
                List<Transaction> transactions = new();
            
                foreach (var exchange in Exchange.Values)
                {
                    transactions.AddRange(exchange.Transactions);
                }
                
                return transactions;
            }
            set
            {
                foreach (var transaction in value)
                {
                    Exchange.TryAdd(transaction.Exchange, new Exchange());
                    Exchange[transaction.Exchange].Transactions.Add(transaction);
                }
            }
        }
        
        public override List<Order> Orders
        {
            get
            {
                List<Order> orders = new();
                
                foreach (var exchange in Exchange.Values)
                {
                    orders.AddRange(exchange.Orders);
                }

                return orders;
            }
            set
            {
                foreach (var order in value)
                {
                    Exchange.TryAdd(order.Exchange, new Exchange());
                    Exchange[order.Exchange].NewOrder(order);
                }
            }
        }

        public override bool DeleteOrder(Guid id, string user)
        {
            return Exchange.Values.Any(market => market.DeleteOrder(id, user));
        }

        public override List<Transaction>? NewOrder(Order newOrder)
        {
            if (!Exchanges.Contains(newOrder.Exchange)) return null;

            Exchange.TryAdd(newOrder.Exchange, new Exchange());
            
            var transactions = Exchange[newOrder.Exchange].NewOrder(newOrder);
            return transactions;
        }

        public override void Clear()
        {
            Exchanges.Clear();
            Orders.Clear();
            Transactions.Clear();
        }
    }
}
