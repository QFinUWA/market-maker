using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Hubs
{
    public interface IMarketClient
    {
        Task ReceiveMessage(string message);
        Task NewOrder(NewOrderResponse orderResponse);
        Task DeletedOrder(Guid id);
        Task TransactionEvent(TransactionResponse transaction);
        Task UserJoined(string id);
        Task MarketState(MarketStateResponse orderState);
        Task MarketConfig(MarketConfigResponse orderState);
        Task StateUpdated(string newState);
        Task MarketCreated(string marketCode);
    }
}
