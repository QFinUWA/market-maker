namespace MarketMaker.Services
{
    public class MarketGroup
    {
        public Dictionary<string, LocalMarketService> Markets { get; } = new();

        public void DeleteMarket(string marketCode)
        {
            if (!Markets.ContainsKey(marketCode)) return;

            Markets.Remove(marketCode);
        }

    }
}
