using MarketMaker.Contracts;
using MarketMaker.Models;
using System.Text.Json.Serialization;

namespace MarketMaker.Services
{
    [Serializable]
    public abstract class MarketService
    {
        public abstract List<Order> Orders { get; set;  }
        public abstract List<Transaction> Transactions { get; set; }
        public MarketConfig Config { get; set; } = new();
        
        public MarketState State { get; set; } = MarketState.Lobby;
        
        [JsonIgnore] 
        public List<string> Exchanges => Config.ExchangeNames.Keys.ToList();

        public string AddExchange()
        {
            var i = Exchanges.Count;
            var code = ((char)('A' + i % 26)).ToString();

            if (Config.ExchangeNames.ContainsKey(code)) throw new Exception("Maximum of 26 markets allowed");
            
            Config.ExchangeNames[code] = null;

            return code;
        }

        public bool UpdateConfig(ConfigUpdateRequest updateRequest)
        {
            if (updateRequest.ExchangeNames != null)
            {
                if (updateRequest.ExchangeNames.Keys.Any(exchangeCode => !Exchanges.Contains(exchangeCode)))
                {
                    return false;
                }
            }
            
            Config.Update(updateRequest);
            return true;
        }
        
        public abstract List<Transaction>? NewOrder(Order newOrder);
        public abstract bool DeleteOrder(Guid id, string user);

        public abstract void Clear();

    }
}
