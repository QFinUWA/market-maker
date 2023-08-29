using MarketMaker.Contracts;
using MarketMakerAPI.Contracts;
using MarketMakerAPI.Models;

namespace MarketMaker.Hubs
{
    public interface IMarketClient
    {
        Task RecieveMessage(string message);
        Task NewOrder(NewOrderResponse orderResponse);
        Task DeletedOrder(Guid message);
        Task AmmendedOrder(NewOrderResponse orderResponse);
        Task OrderHit(string message);
        Task UserJoined(string id);
        Task MarketState(MarketStateResponse marketState);
    }
}
