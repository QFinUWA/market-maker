using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services;

public class ResponseConstructor
{
    private readonly MarketGroup _marketGroup;
    private readonly IUserService _userService;

    public ResponseConstructor(MarketGroup marketGroup, IUserService userService)
    {
        this._marketGroup = marketGroup;
        this._userService = userService;
    }
    
    public LobbyStateResponse LobbyState(string gameCode)
    {
        var marketService = _marketGroup.Markets[gameCode];

        var marketParticipants = _userService
            .GetUsers(gameCode)
            .Where(user => user.Name != null)
            .Select(user => user.Name ?? "") // won't ever be null but this will shut my IDE up
            .ToList();

        var exchangeNames = marketService.Config.ExchangeNames
            .Select(e => (e.Key, e.Value))
            .ToList();
        
        return new LobbyStateResponse(
            exchangeNames,
            marketParticipants,
            marketService.State.ToString(),
            marketService.Config.MarketName ?? "unnamed market",
            gameCode
        );
    }
    
    public MarketStateResponse MarketState(string gameCode)
    {
        var marketService = _marketGroup.Markets[gameCode];
        
        return new MarketStateResponse(
            marketService.Orders,
            marketService.Transactions
        );
    }

    public NewOrderResponse NewOrder(Order newOrder)
    {
        return new NewOrderResponse(
            newOrder.User,
            newOrder.Exchange,
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
            transaction.Exchange,
            transaction.Price,
            transaction.Quantity,
            transaction.Aggressor,
            transaction.TimeStamp
        );
    }

}