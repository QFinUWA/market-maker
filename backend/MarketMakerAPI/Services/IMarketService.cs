using MarketMaker.Models;

namespace MarketMaker.Services
{
    public interface IMarketService

    {
        List<Order> GetOrders();
        List<Order> NewOrder(Order order);
        // Order UpdateOrder(Order order);
        void DeleteOrder(string market, Guid id);

        void AddExchange(string market);

        Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);

        List<string> Exchanges {  get; }


    }
}
