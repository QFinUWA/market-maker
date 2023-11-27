namespace MarketMaker.Services
{
    public class MarketGroup
    {
        public Dictionary<string, LocalMarketService> Markets { get; }

        public MarketGroup() 
        {
            Markets = new();
        }
    }
}
