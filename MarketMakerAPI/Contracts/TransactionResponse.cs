namespace MarketMaker.Contracts;

public record TransactionResponse(
        string BuyerUser,
        Guid BuyerOrderId,
        string SellerUser,
        Guid SellerOrderId,
        string market,
        int Price,
        int Quantity,
        string Aggressor,
        DateTime TimeStamp
    );