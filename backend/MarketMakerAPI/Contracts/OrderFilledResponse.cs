namespace MarketMaker.Contracts
{
    public record OrderFilledResponse(
           Guid id,
           int newQuantity
        );

}
