namespace MarketMaker.Contracts;

public record OrderReceivedResponse(
    List<Guid> CreatedOrders, 
    string RequestReference
);