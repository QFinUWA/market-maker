using System.Collections.Concurrent;
using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMaker.Models;
using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs
{
    public sealed class MarketHub: Hub<IMarketClient>
    {
        private readonly MarketGroup _marketServices;
        private readonly IUserService _userService;
        private const int MarketCodeLength = 5;
        private readonly Random _random;
        private readonly ResponseConstructor _responseConstructor;
        private readonly Dictionary<string, CancellationTokenSource> _marketCancellationTokens;
        private const int EmptyMarketLifetimeMinutes = 60;

        public MarketHub(MarketGroup marketServices, IUserService userService, Dictionary<string, CancellationTokenSource> cancellationTokens) 
        {
            _marketServices = marketServices;
            _userService = userService;
            _random = new Random();
            _responseConstructor = new ResponseConstructor(_marketServices, _userService);
            _marketCancellationTokens = cancellationTokens;
        }


        public override async Task OnConnectedAsync()
        {
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? e)
        {
            User user;
            try
            {
                user = _userService.GetUser(Context.ConnectionId);
            }
            catch (Exception) {return;}
                
            user.Connected = false;
            
            var group = user.Market;

            if (_userService.GetUsers(group).Any(u => u.Connected)) return;

            var source = new CancellationTokenSource();
                
            _marketCancellationTokens[group] = source;
            
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(EmptyMarketLifetimeMinutes), source.Token);
                _marketServices.DeleteMarket(group);
                _userService.DeleteUsers(group);
            }
            catch (AggregateException ae)
            {
                foreach (var ie in ae.InnerExceptions)
                   await Clients.All.ReceiveMessage($"{ie.GetType().Name}, {ie.Message}");
            }
            
            await Clients.All.ReceiveMessage("Disposing of token");
            source.Dispose();

            _marketCancellationTokens.Remove(group);

        }
        public async Task MakeNewMarket()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringChars = new char[MarketCodeLength];
            
            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[_random.Next(chars.Length)];
            var marketCode = new string(stringChars);

            // create new market service
            var marketService = new LocalMarketService();
            _marketServices.Markets[marketCode] = marketService;

            // make user an admin
            _userService.AddAdmin(Context.ConnectionId, marketCode);

            await Clients.Caller.LobbyState(_responseConstructor.LobbyState(marketCode));
            await Groups.AddToGroupAsync(Context.ConnectionId, marketCode);
        }

        public async Task MakeNewExchange()
        {
            var user = _userService.GetUser(Context.ConnectionId, admin: true);
            var group = user.Market;

            MarketService marketService = _marketServices.Markets[group];
            
            if (marketService.State != MarketState.Lobby)
                throw new Exception("Cannot add Exchange while game in progress");

            // add new exchange
             marketService.AddExchange();
            
            // notify clients
            
            await Clients.Group(group).LobbyState(_responseConstructor.LobbyState(group));
        }

        public async Task UpdateConfig(ConfigUpdateRequest configUpdate)
        {
            var user = _userService.GetUser(Context.ConnectionId, admin: true);
            
            var group = user.Market;
            
            MarketService marketService = _marketServices.Markets[group];

            if (marketService.State != MarketState.Lobby)
                throw new Exception("Cannot update config while game in progress");

            marketService.UpdateConfig(configUpdate);

            await Clients.Group(group).LobbyState(_responseConstructor.LobbyState(group));
        }

        public async Task JoinMarketLobby(string groupName)
        {
            if (groupName.Length != MarketCodeLength || !groupName.All(Char.IsLetter)) 
                throw new Exception("Invalid Group ID");
            
            var groupNameUpper = groupName.ToUpper();

            if (!_marketServices.Markets.ContainsKey(groupNameUpper)) 
                throw new Exception("Group doesn't exist");
            
            var user = _userService.AddUser(groupNameUpper, Context.ConnectionId);

            if (_marketCancellationTokens.ContainsKey(groupName))
            {
                var token = _marketCancellationTokens[groupName];
                token.Cancel();
                token.Dispose();
                _marketCancellationTokens.Remove(user.Market);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupNameUpper);
            await Clients.Group(groupName).LobbyState(_responseConstructor.LobbyState(groupName));
            await Clients.Caller.MarketState(_responseConstructor.MarketState(groupName));
        }
        
        public async Task UpdateMarketState(string newStateString)
        {
            var user = _userService.GetUser(Context.ConnectionId, admin:true);

            var marketCode = user.Market;
            
            var stateExists = Enum.TryParse(newStateString, true, out MarketState newState);
            if (!stateExists) throw new Exception("Invalid state");
                
            var marketService = _marketServices.Markets[marketCode];
            
            try
            {
                marketService.State = newState;
            }
            catch (ArgumentException e)
            {
                throw new Exception(e.Message);
            }
            
            await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
        }

        public async Task JoinMarket(string username)
        {
            var user = _userService.GetUser(Context.ConnectionId);
            if (username.Length == 0) throw new Exception("Username must be at least 1 character long");

            user.Name = username;
            
            var marketCode = user.Market;
            
            // TODO: retrieve cookie/local storage/claim etc
                        
            await Clients.Group(marketCode).NewParticipant(username);
        }

        public async Task DeleteOrder(Guid orderId)
        {
            var user = _userService.GetUser(Context.ConnectionId);
            var group = user.Market;

            var username = user.Name;
            if (username == null) throw new Exception("You are not a participant");
            
            MarketService marketService = _marketServices.Markets[group];

            if (marketService.State != MarketState.Open) throw new Exception("Market is not open");
            
            var orderDeleted = marketService.DeleteOrder(orderId, username);
            if (!orderDeleted) throw new Exception("Order deletion rejected");
            
            await Clients.Group(group).DeletedOrder(orderId);
            
        }

        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            if (price <= 0) throw new Exception("Price must be > 0");
            
            var user = _userService.GetUser(Context.ConnectionId);
            var username = user.Name;
            if (username == null) throw new Exception("You are not a participant");
            
            var groupName = user.Market;
            
            MarketService marketService = _marketServices.Markets[groupName];
            if (marketService.State != MarketState.Open) throw new Exception("Market is not open");

            var newOrder = new Order(
                username,
                exchange,
                price,
                quantity
            );

            var originalOrder = (Order)newOrder.Clone();

            var transactions = marketService.NewOrder(newOrder);
            if (transactions == null) throw new Exception("Invalid Order");
            
            await Clients.Group(groupName).NewOrder(_responseConstructor.NewOrder(originalOrder));
            
            var orderFilledTask = transactions.Select<Transaction, Task>(transaction =>
                Clients.Group(groupName).TransactionEvent(_responseConstructor.Transaction(transaction))
            );

            await Task.WhenAll(orderFilledTask);
        }

        public async Task CloseMarket(Dictionary<string, int>? closePrices= null) 
        {
            
            var user = _userService.GetUser(Context.ConnectionId, admin: true);
            
            var marketCode = user.Market;
            
            MarketService marketService = _marketServices.Markets[marketCode];
            if (marketService.State == MarketState.Lobby) throw new Exception("Market cannot be closed from the lobby");

            marketService.State = MarketState.Closed;

            if (closePrices == null)
            {
                marketService.Clear();
                await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
                return;
            }
            
            if (!(closePrices.Keys.All(marketService.Exchanges.Contains) 
                && closePrices.Count == marketService.Exchanges.Count))
            {
                throw new Exception("Incorrect exchange names");
            }

            if (closePrices.Values.Any(price => price < 0))
                throw new Exception("Price must be positive");
            
            marketService.Clear();
            await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
            await Clients.Group(marketCode).ClosingPrices(closePrices);
        }
    }
}
