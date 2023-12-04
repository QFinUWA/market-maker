using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalExchangeService : ExchangeService
    {
        protected readonly Dictionary<string, Market> Market = new();

        public override List<Transaction> Transactions
        {
            get
            {
                List<Transaction> transactions = new();
            
                foreach (var market in Market.Values)
                {
                    transactions.AddRange(market.Transactions);
                }
                
                return transactions;
            }
            set
            {
                foreach (var transaction in value)
                {
                    Market.TryAdd(transaction.Market, new Market());
                    Market[transaction.Market].Transactions.Add(transaction);
                }
            }
        }
        
        public override List<Order> Orders
        {
            get
            {
                List<Order> orders = new();
                
                foreach (var market in Market.Values)
                {
                    orders.AddRange(market.Orders);
                }

                return orders;
            }
            set
            {
                foreach (var order in value)
                {
                    Market.TryAdd(order.Market, new Market());
                    Market[order.Market].NewOrder(order);
                }
            }
        }

        public override bool DeleteOrder(Guid id, string user)
        {
            return Market.Values.Any(exchange => exchange.DeleteOrder(id, user));
        }

        public override List<Transaction>? NewOrder(Order newOrder)
        {
            if (!Markets.Contains(newOrder.Market)) return null;

            Market.TryAdd(newOrder.Market, new Market());
            
            var transactions = Market[newOrder.Market].NewOrder(newOrder);
            return transactions;
        }

        public override void Clear()
        {
            Markets.Clear();
            Orders.Clear();
            Transactions.Clear();
        }
    }
}
