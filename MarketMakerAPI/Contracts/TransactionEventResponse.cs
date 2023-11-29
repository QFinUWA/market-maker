namespace MarketMaker.Contracts;

public record TransactionEventResponse(
        string BuyerUser,
        Guid BuyerOrderId,
        string SellerUser,
        Guid SellerOrderId,
        int Price,
        int Quantity,
        string Aggressor,
        DateTime TimeStamp
    );