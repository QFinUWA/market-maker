namespace MarketMaker.Contracts
{
    public record DeleteOrderRequest(
           string exchange,
           Guid Id
        );
}