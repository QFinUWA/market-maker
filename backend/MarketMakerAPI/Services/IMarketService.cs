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

        List<string> Exchanges {  get; }


    }
}
