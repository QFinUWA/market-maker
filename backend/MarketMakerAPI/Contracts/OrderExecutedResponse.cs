namespace MarketMakerAPI.Contracts
{
    public record OrderExecutedResponse(
           Guid Id,
           int volumeFilled
        );

}
