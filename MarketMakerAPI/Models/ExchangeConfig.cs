using MarketMaker.Contracts;

namespace MarketMaker.Models;

[Serializable]
public class ExchangeConfig
{
    public string? ExchangeName { get; set; }
    public Dictionary<string, string?> MarketNames { get; set; } = new();

    public void Update(ConfigUpdateRequest updateRequest)
    {
        if (updateRequest.ExchangeCode != null) ExchangeName = updateRequest.ExchangeCode;

        if (updateRequest.MarketNames != null)
            foreach (var (key, name) in updateRequest.MarketNames)
                MarketNames[key] = name;
    }
}