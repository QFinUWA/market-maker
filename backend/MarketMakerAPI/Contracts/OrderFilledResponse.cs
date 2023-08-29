namespace MarketMakerAPI.Contracts
{
    public record OrderFilledResponse(
           string market,
           Guid id,
           int newQuantity
        );

}
