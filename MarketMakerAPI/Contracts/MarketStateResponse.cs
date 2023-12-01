using MarketMaker.Models;

namespace MarketMaker.Contracts
{
    public record MarketStateResponse(
        List<Order> Orders,
        List<Transaction> Transactions
        );
    
    // TODO: add # of users spectating 

}