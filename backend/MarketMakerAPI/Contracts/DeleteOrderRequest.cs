namespace MarketMakerAPI.Contracts
{
    public record DeleteOrderRequest(
           string market,
           Guid Id
        );

}
