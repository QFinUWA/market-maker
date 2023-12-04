namespace MarketMaker.Contracts
{
    public record NewOrderResponse(
           string User,
           string Market,
           int Price,
           int Quantity,
           DateTime TimeStamp,
           Guid Id
        );

}
