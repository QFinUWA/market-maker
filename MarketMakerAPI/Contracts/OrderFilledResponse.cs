namespace MarketMaker.Contracts
{
    public record OrderFilledResponse(
           Guid Id,
           int NewQuantity
        );

}
