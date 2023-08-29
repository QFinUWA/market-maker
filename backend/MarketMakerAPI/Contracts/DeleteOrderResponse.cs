namespace MarketMaker.Contracts
{
    public record DeleteOrderResponse(
           string market,
           Guid Id
        );

}
