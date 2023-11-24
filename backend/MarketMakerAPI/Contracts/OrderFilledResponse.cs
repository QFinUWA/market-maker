namespace MarketMaker.Contracts
{
    public record OrderFilledResponse(
           string exchange,
           Guid id,
           string user,
           int atPrice,
           int newQuantity
        );

}
