namespace MarketMaker.Contracts
{
    public record NewOrderResponse(
           string User,
           string Exchange,
           int Price,
           int Quantity,
           DateTime TimeStamp,
           Guid Id
        );

}
