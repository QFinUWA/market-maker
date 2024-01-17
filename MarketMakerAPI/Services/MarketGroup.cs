namespace MarketMaker.Services;

public class ExchangeGroup
{
    private const int ExchangeCodeLength = 5;
    public Dictionary<string, LocalExchangeService> Exchanges { get; } = new();
    public void DeleteExchange(string exchangeCode)
    {
        if (!Exchanges.ContainsKey(exchangeCode)) return;
        Exchanges.Remove(exchangeCode);
    }

    public string AddExchange()
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      var stringChars = new char[ExchangeCodeLength];
      Random random = new();
      for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[random.Next(chars.Length)];
      var exchangeCode = new string(stringChars);
      Exchanges[exchangeCode] = new LocalExchangeService();

      return exchangeCode;
    } 
}