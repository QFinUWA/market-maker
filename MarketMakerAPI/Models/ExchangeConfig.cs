using MarketMaker.Contracts;

namespace MarketMaker.Models;

[Serializable]
public class ExchangeConfig
{
        public string? ExchangeName { get; set; }
        public Dictionary<string, string?> MarketNames { get; set; }= new();

        public void Update(ConfigUpdateRequest updateRequest)
        {
            if (updateRequest.ExchangeName != null)
            {
                ExchangeName = updateRequest.ExchangeName;
            }

            if (updateRequest.MarketNames != null)
            {
                foreach (var (key, name) in updateRequest.MarketNames)
                {
                    MarketNames[key] = name;
                }
            }
        }
}