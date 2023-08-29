using MarketMakerAPI.Models;

namespace MarketMakerAPI.Services
{
    public interface IMarketService
    {
        List<Order> Orders { get; }

        void NewOrder(Order order);
        Order UpdateOrder(Order order);
        void DeleteOrder(Guid id);

    }
}
