namespace MarketMaker.Contracts
{
    public record DeleteOrderRequest(
           string Exchange,
           Guid Id
        );
}