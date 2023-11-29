using MarketMaker.Models;

namespace MarketMaker.Services
{
    public abstract class MarketService
    {
        public abstract List<string> Exchanges {  get; }
        public abstract List<string> Participants { get; }
        public abstract List<Order> Orders { get; }
        public abstract List<Transaction> Transactions { get; }

        public MarketConfig Config { get; } = new();

        private MarketState _state = MarketState.Lobby;

        public MarketState State
        {
            get => _state;
            set
            {
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
                        if (value == MarketState.Open) break;
                        throw new ArgumentException("Closed state can only transition to Open");
                    default:
                        return;
                }

                _state = value;
            }
        }

        public abstract bool AddParticipant(string username);
        public abstract List<Transaction>? NewOrder(Order newOrder);
        public abstract bool DeleteOrder(Guid id, string user);
        public abstract bool AddExchange(string market);
        public abstract Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);
    }
}
