using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services;

public class ResponseConstructor
{
    private readonly ExchangeGroup _exchangeGroup;
    private readonly IUserService _userService;

    public ResponseConstructor(ExchangeGroup exchangeGroup, IUserService userService)
    {
        _exchangeGroup = exchangeGroup;
        _userService = userService;
    }
    
    public LobbyStateResponse LobbyState(string gameCode)
    {
        var exchangeService = _exchangeGroup.Exchanges[gameCode];

        var exchangeParticipants = _userService
            .GetUsers(gameCode)
            .Where(user => user.Name != null)
            .Select(user => user.Name!)
            .ToList();

        var marketNames = exchangeService.Config.MarketNames
            .Select(e => new List<string?> {e.Key, e.Value})
            .ToList();
        
        return new LobbyStateResponse(
            marketNames,
            exchangeParticipants,
            exchangeService.State.ToString(),
            exchangeService.Config.ExchangeName ?? "unnamed exchange",
            gameCode
        );
    }
    
    public ExchangeStateResponse ExchangeState(string gameCode)
    {
        var exchangeService = _exchangeGroup.Exchanges[gameCode];
        
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