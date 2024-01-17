using MarketMaker.Contracts;
using MarketMaker.Models;

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

    public NewOrderResponse NewOrder(Order newOrder)
    {
        return new NewOrderResponse(
            newOrder.User,
            newOrder.Market,
            newOrder.Price,
            newOrder.Quantity,
            newOrder.TimeStamp,
            newOrder.Id
        );
    }

    public TransactionResponse Transaction(Transaction transaction)
    {
        return new TransactionResponse(
            transaction.BuyerUser,
            transaction.BuyerOrderId,
            transaction.SellerUser,
            transaction.SellerOrderId,
            transaction.Market,
            transaction.Price,
            transaction.Quantity,
            transaction.Aggressor,
            transaction.TimeStamp
        );
    }
}