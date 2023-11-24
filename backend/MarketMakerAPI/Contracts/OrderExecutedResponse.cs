namespace MarketMaker.Contracts
{
    public record OrderExecutedResponse(
           Guid Id,
           int volumeFilled
        );

}
