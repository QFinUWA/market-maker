using MarketMaker.Contracts;
using MarketMaker.Models;
using Microsoft.AspNetCore.Identity;

namespace MarketMaker.Services;

public class ResponseConstructor
{
    private readonly ExchangeGroup _exchangeGroup;

    public ResponseConstructor(ExchangeGroup exchangeGroup)
    {
        _exchangeGroup = exchangeGroup;
    }

    public LobbyStateResponse LobbyState(string exchangeCode)
    {
        var exchangeService = _exchangeGroup.Exchanges[exchangeCode];

        var exchangeParticipants = exchangeService.Users.Values.ToList();

        var marketNames = exchangeService.Config.MarketNames
            .Select(e => new List<string?> { e.Key, e.Value })
            .ToList();

        return new LobbyStateResponse(
            marketNames,
            exchangeParticipants,
            exchangeService.State.ToString(),
            exchangeService.Config.ExchangeName ?? "unnamed exchange",
            exchangeCode
        );
    }

    public ExchangeStateResponse ExchangeState(string exchangeCode)
    {
        var exchangeService = _exchangeGroup.Exchanges[exchangeCode];

        return new ExchangeStateResponse(
            exchangeService.Orders,
            exchangeService.Transactions
        );
    }

    public NewOrderResponse NewOrder(Order order, List<Transaction> tradedOrders)
    {
        var (id, user, price, market, quantity, timeStamp) = order;
        return new NewOrderResponse(user, market, price, quantity, timeStamp, id, tradedOrders);
    }

    public OrderReceivedResponse OrderReceived(List<Guid> ids, string requestReference)
    {
        return new OrderReceivedResponse(ids, requestReference);
    }

}