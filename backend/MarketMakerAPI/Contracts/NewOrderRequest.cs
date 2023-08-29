namespace MarketMakerAPI.Contracts
{
    public record NewOrderRequest(
           string Market,
           int Price,
           int Quantity
    );
}
