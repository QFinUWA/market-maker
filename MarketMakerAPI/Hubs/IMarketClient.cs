using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Hubs
{
    public interface IMarketClient
    {
        Task ReceiveMessage(string message);
        Task NewOrder(NewOrderResponse orderResponse);
        Task DeletedOrder(Guid id);
        Task TransactionEvent(TransactionEvent transactionEvent);
        Task UserJoined(string id);
        Task MargetConfig(MarketConfigResponse marketConfig);
        Task MarketState(MarketStateResponse orderState);
        Task MarketConfig(MarketConfigResponse orderState);
    }
}
