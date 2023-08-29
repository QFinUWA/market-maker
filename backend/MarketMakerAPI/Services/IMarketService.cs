using MarketMakerAPI.Models;

namespace MarketMakerAPI.Services
{
    public interface IMarketService
    {
        List<Order> GetOrders();
        List<Order> NewOrder(Order order);
        // Order UpdateOrder(Order order);
        void DeleteOrder(string market, Guid id);

    }
}
