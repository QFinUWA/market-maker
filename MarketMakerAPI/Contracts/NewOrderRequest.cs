namespace MarketMaker.Contracts
{
    public record NewOrderRequest(
           string Exchange,
           int Price,
           int Quantity
    );
}
