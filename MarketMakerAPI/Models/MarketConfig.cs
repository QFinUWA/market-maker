using MarketMaker.Contracts;

namespace MarketMaker.Models;

public class MarketConfig
{
        public string? MarketName { get; set; }
        public readonly Dictionary<string, string?> ExchangeNames = new();

        public void Update(ConfigUpdateRequest updateRequest)
        {
            MarketName = updateRequest.MarketName;
            foreach (var (key, name) in updateRequest.ExchangeNames)
            {
                if (!ExchangeNames.ContainsKey(key)) 
                    throw new ArgumentException($"\"{key}\" is not a valid exchange");
                ExchangeNames[key] = name;
            }
        }
}