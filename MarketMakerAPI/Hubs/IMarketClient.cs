using MarketMaker.Contracts;

namespace MarketMaker.Hubs;

public interface IExchangeClient
{
    Task ReceiveMessage(string message);
    Task NewOrder(NewOrderResponse orderResponse);
    Task DeletedOrder(Guid id);
    Task TransactionEvent(TransactionResponse transaction);
    Task ExchangeState(ExchangeStateResponse orderState);
    Task LobbyState(LobbyStateResponse lobbyState);
    Task NewParticipant(string username);
    Task StateUpdated(string newState);
    Task ClosingPrices(Dictionary<string, int> closingPrices);
}