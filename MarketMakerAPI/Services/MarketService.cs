using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services
{
    public abstract class MarketService
    {
        public abstract List<Order> Orders { get; }
        public abstract List<Transaction> Transactions { get; }

        public readonly MarketConfig Config = new();

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
            if (updateRequest.ExchangeNames.Keys.Any(exchangeCode => !Exchanges.Contains(exchangeCode)))
            {
                return false;
            }
            
            Config.Update(updateRequest);
            return true;
        }

        private MarketState _state = MarketState.Lobby;

        public MarketState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                
                switch (_state)
                {
                    case MarketState.Lobby:
                        if (value == MarketState.Open) break;
                        throw new ArgumentException("Lobby state can only transition to Open");
                    case MarketState.Open:
                        if (value == MarketState.Paused) break;
                        if (value == MarketState.Closed) break;
                        throw new ArgumentException("Open state can only transition to Paused or Closed");
                    case MarketState.Paused:
                        if (value == MarketState.Open) break;
                        if (value == MarketState.Closed) break;
                        throw new ArgumentException("Paused state can only transition to Open or Closed");
                    case MarketState.Closed:
                        if (value == MarketState.Lobby) break;
                        throw new ArgumentException("Closed state can only transition to Open");
                    default:
                        return;
                }

                _state = value;
            }
        }
        public abstract List<Transaction>? NewOrder(Order newOrder);
        public abstract bool DeleteOrder(Guid id, string user);
        public abstract Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);
    }
}
