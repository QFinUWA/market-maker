using System.Collections.Concurrent;

namespace MarketMaker.Services;

public class ExchangeGroup
{
    private const int ExchangeCodeLength = 5;
    private const int CodeGenerationMaxRetries = 100;
    public ConcurrentDictionary<string, LocalExchangeService> Exchanges { get; } = new();
    
    public void DeleteExchange(string exchangeCode)
    {
        if (!Exchanges.ContainsKey(exchangeCode)) return;
        Exchanges.TryRemove(exchangeCode, out var removed);
        
        if (removed is null) throw new Exception();
    }

    public string AddExchange()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Random random = new();
        var stringChars = new char[ExchangeCodeLength];

        for (int retries = 0; retries < CodeGenerationMaxRetries; retries++)
        {
            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[random.Next(chars.Length)];
            var exchangeCode = new string(stringChars);

            if (Exchanges.ContainsKey(exchangeCode)) continue;

            var wasAdded = Exchanges.TryAdd(exchangeCode, new LocalExchangeService());
            if (!wasAdded) throw new Exception();

            return exchangeCode;
        }
        throw new Exception();
    }

}