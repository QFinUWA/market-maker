namespace MarketMaker.Contracts
{
    public record DeleteOrderRequest(
           string market,
           Guid Id
        );

}
