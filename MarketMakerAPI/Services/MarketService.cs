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
                        return;
                    case MarketState.Open:
                        if (value == MarketState.Paused) break;
                        if (value == MarketState.Closed) break;
                        return;
                    case MarketState.Paused:
                        if (value == MarketState.Open) break;
                        if (value == MarketState.Closed) break;
                        return;
                    case MarketState.Closed:
                        if (value == MarketState.Open) break;
                        return;
                    default:
                        return;
                }

                _state = value;
            }
        }

        public abstract void AddParticipant(string username);
        public abstract (Order, List<Transaction>) NewOrder(string username, string exchange, int price, int quantity);
        public abstract void DeleteOrder(Guid id, string user);
        public abstract void AddExchange(string market);
        public abstract Dictionary<string, float> CloseMarket(Dictionary<string, int> prices);
    }
}
