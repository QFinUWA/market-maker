using MarketMaker.Models;

namespace MarketMaker.Contracts
{
    public record MarketStateResponse(
        List<string> Users,
        List<Order> Orders,
        string MarketName,
        List<string> Exchanges
        );
    
    // TODO: add # of users spectating 

}