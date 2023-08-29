using MarketMakerAPI.Models;

namespace MarketMaker.Contracts
{
    public record MarketStateResponse(List<string> Users, List<Order> Orders);
  

}