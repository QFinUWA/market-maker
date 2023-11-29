using MarketMaker.Models;

namespace MarketMaker.Contracts
{
    public record MarketStateResponse(
        List<string> Users,
        List<Order> Orders,
        List<Transaction> Transactions,
        string State
        );
    
    // TODO: add # of users spectating 

}