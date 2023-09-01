namespace MarketMaker.Contracts
{
    public record NewOrderRequest(
           string exchange,
           int Price,
           int Quantity
    );
}
