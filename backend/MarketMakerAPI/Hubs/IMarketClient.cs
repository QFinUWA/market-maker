using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Hubs
{
    public interface IMarketClient
    {
        Task RecieveMessage(string message);
        Task NewOrder(NewOrderResponse orderResponse);
        Task DeletedOrder(DeleteOrderResponse message);
        Task AmmendedOrder(NewOrderResponse orderRespose);
        Task OrderFilled(OrderFilledResponse orderFilledResponse);
        Task UserJoined(string id);
        Task MarketState(MarketStateResponse marketState);
        Task ExchangeAdded(string name);
        Task MarketAdded(string marketName);
    }
}
