namespace MarketMaker.Models;

public record Transaction(
    string BuyerUser,
    Guid BuyerOrderId,
    string SellerUser,
    Guid SellerOrderId,
    string Market,
    long Price,
    int Quantity,
    Guid PassiveOrder,
    DateTime TimeStamp 
);