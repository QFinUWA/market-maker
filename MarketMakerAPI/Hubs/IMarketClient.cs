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
        Task MarketState(MarketStateResponse orderState);
        Task LobbyState(LobbyStateResponse lobbyState);
        Task NewParticipant(string username);
        Task StateUpdated(string newState);
        Task ClosingPrices(Dictionary<string, int> closingPrices);
    }
}
