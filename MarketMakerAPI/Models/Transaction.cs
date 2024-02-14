namespace MarketMaker.Models;

public record Transaction(
    string BuyerUser,
    Guid BuyerOrderId,
    string SellerUser,
    Guid SellerOrderId,
    string Market,
    int Price,
    int Quantity,
    Guid PassiveOrder,
    DateTime TimeStamp 
);