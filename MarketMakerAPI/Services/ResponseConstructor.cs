using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services;

public static class ResponseConstructor
{
    
    public static MarketConfigResponse MarketConfig(MarketService marketService)
    {
        return new MarketConfigResponse(
            marketService.Config.MarketName,
            marketService.Exchanges.ToList()
        );
    }
    
    public static MarketStateResponse MarketState(MarketService marketService)
    {
        return new MarketStateResponse(
            marketService.Participants,
            marketService.Orders,
            marketService.Transactions,
            marketService.State.ToString()
        );
    }

    public static NewOrderResponse NewOrder(Order newOrder)
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

    public static TransactionResponse Transaction(Transaction transaction)
    {
        return new TransactionResponse(
            transaction.BuyerUser,
            transaction.BuyerOrderId,
            transaction.SellerUser,
            transaction.SellerOrderId,
            transaction.Price,
            transaction.Quantity,
            transaction.Aggressor,
            transaction.TimeStamp
        );
    }

}