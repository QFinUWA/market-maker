namespace MarketMaker.Contracts;

public record OrderReceivedResponse(
    Guid CreatedOrder, 
    string RequestReference
);