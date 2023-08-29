namespace MarketMakerAPI.Contracts
{
    public record NewOrderResponse(
           string User,
           string Market,
           int Price,
           int Quantity,
           DateTime CreatedAt,
           Guid Id
        );

}
