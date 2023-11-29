using MarketMaker.Models;

namespace MarketMaker.Services
{
    public interface IMarketService

    {
        List<Order> GetOrders();
        List<TransactionEvent> GetTransactions();
        (Order, List<TransactionEvent>) NewOrder(string username, string exchange, int price, int quantity);
        // Order UpdateOrder(Order order);
        void DeleteOrder(Guid id, string user);

        void AddExchange(string market);

        Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);

        IEnumerable<string> Exchanges {  get; }
        IEnumerable<string> Participants { get; }


    }
}
