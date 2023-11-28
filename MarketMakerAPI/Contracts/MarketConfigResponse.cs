using MarketMaker.Models;

namespace MarketMaker.Contracts
{
    public record MarketConfigResponse(
        string MarketName,
        List<string> Exchanges
        );
    
}