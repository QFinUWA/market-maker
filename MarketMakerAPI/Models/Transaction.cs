namespace MarketMaker.Models;

public record Transaction(
        string BuyerUser,
        Guid BuyerOrderId,
        string SellerUser,
        Guid SellerOrderId,
        int Price,
        int Quantity,
        string Aggressor,
        DateTime TimeStamp

    );