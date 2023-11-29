using MarketMaker.Models;

namespace MarketMaker.Services
{
    public interface IMarketService

    {

        (Order, List<TransactionEvent>) NewOrder(string username, string exchange, int price, int quantity);
        // Order UpdateOrder(Order order);
        void DeleteOrder(Guid id, string user);

        void AddExchange(string market);

        Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);

        List<string> Exchanges {  get; }
        List<string> Participants { get; }
        List<Order> Orders { get; }
        List<TransactionEvent> Transactions { get; }

        void AddParticipant(string username);

    }
}
