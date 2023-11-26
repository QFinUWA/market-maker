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
        Task MarketState(MarketStateResponse marketState);
        Task ExchangeAdded(string name);
    }
}
