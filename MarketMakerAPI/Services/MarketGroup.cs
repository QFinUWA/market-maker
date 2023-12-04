namespace MarketMaker.Services
{
    public class ExchangeGroup
    {
        public Dictionary<string, LocalExchangeService> Exchanges { get; } = new();

        public void DeleteExchange(string exchangeCode)
        {
            if (!Exchanges.ContainsKey(exchangeCode)) return;

            Exchanges.Remove(exchangeCode);
        }

    }
}
