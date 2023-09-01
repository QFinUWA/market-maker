namespace MarketMaker.Contracts
{
    public record DeleteOrderResponse(
           string exchange,
           Guid Id
        );

}
