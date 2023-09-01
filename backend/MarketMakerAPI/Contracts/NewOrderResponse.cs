namespace MarketMaker.Contracts
{
    public record NewOrderResponse(
           string User,
           string exchange,
           int Price,
           int Quantity,
           DateTime CreatedAt,
           Guid Id
        );

}
