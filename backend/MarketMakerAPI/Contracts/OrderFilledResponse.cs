namespace MarketMaker.Contracts
{
    public record OrderFilledResponse(
           string market,
           Guid id,
           int newQuantity
        );

}
