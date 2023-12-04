namespace MarketMaker.Contracts
{
    public record DeleteOrderRequest(
           string Market,
           Guid Id
        );
}