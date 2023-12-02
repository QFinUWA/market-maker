using MarketMaker.Contracts;

namespace MarketMaker.Models;

public class MarketConfig
{
        public string? MarketName { get; set; }
        public readonly Dictionary<string, string?> ExchangeNames = new();

        public void Update(ConfigUpdateRequest updateRequest)
        {
            if (updateRequest.MarketName != null)
            {
                MarketName = updateRequest.MarketName;
            }

            if (updateRequest.ExchangeNames != null)
            {
                foreach (var (key, name) in updateRequest.ExchangeNames)
                {
                    ExchangeNames[key] = name;
                }
            }
        }
}