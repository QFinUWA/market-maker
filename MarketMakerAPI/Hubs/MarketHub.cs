using System.Diagnostics;
using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMaker.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

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
        private readonly ILogger<MarketHub> _logger;
        
        public MarketHub(
            MarketGroup marketServices,
            IUserService userService,
            Dictionary<string, CancellationTokenSource> cancellationTokens,
            ILogger<MarketHub> logger
            ) 
        {
            _marketServices = marketServices;
            _userService = userService;
            _random = new Random();
            _responseConstructor = new ResponseConstructor(_marketServices, _userService);
            _marketCancellationTokens = cancellationTokens;
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User Connected");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? e)
        {
            _logger.LogInformation("User Disconnected");
            User user;
            try
            {
                user = _userService.GetUser(Context.ConnectionId);
            }
            catch (Exception) {return;}
                
            user.Connected = false;
            
            var group = user.Market;

            if (_userService.GetUsers(group).Any(u => u.Connected)) return;
            
            _logger.LogInformation($"Lobby {group} empty - starting deletion countdown");            
            var source = new CancellationTokenSource();
                
            _marketCancellationTokens[group] = source;
            
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(EmptyMarketLifetimeMinutes), source.Token);
                _marketServices.DeleteMarket(group);
                _userService.DeleteUsers(group);
            }
            catch (Exception)
            {
                _logger.LogInformation($"Lobby {group} - countdown cancelled");
            }
             
            _logger.LogInformation($"Lobby {group} - countdown complete: deleted");
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

            _marketServices.Markets[marketCode] = new LocalMarketService();

            // make user an admin
            _userService.AddAdmin(Context.ConnectionId, marketCode);
            
            _logger.LogInformation($"Added new market - {marketCode}");
            await Clients.Caller.LobbyState(_responseConstructor.LobbyState(marketCode));
            // await Clients.Caller.MarketState(_responseConstructor.MarketState(marketCode));
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
            var exchangeCode = marketService.AddExchange();
            
            // notify clients
            
            _logger.LogInformation($"Lobby {group} - added exchange {exchangeCode}");
            await Clients.Group(group).LobbyState(_responseConstructor.LobbyState(group));
        }

        public async Task LoadMarket(string jsonSerialized)
        {
            var user = _userService.GetUser(Context.ConnectionId, admin: true);
            var marketCode = user.Market;
            
            var market = JsonSerializer.Deserialize<LocalMarketService>(jsonSerialized);

            if (market == null) throw new Exception("Failed loading market.");
            _marketServices.Markets[marketCode] = market;
            
            _logger.LogInformation($"Loaded market from JSON - {marketCode}");

            await Clients.Group(marketCode).LobbyState(_responseConstructor.LobbyState(marketCode));
            await Clients.Group(marketCode).MarketState(_responseConstructor.MarketState(marketCode));
        }
        
        public async Task UpdateConfig(ConfigUpdateRequest configUpdate)
        {
            var user = _userService.GetUser(Context.ConnectionId, admin: true);
            
            var group = user.Market;
            
            MarketService marketService = _marketServices.Markets[group];

            if (marketService.State != MarketState.Lobby)
                throw new Exception("Cannot update config while game in progress");

            marketService.UpdateConfig(configUpdate);
            
            _logger.LogInformation($"Lobby {group} - updated config"); 
            await Clients.Group(group).LobbyState(_responseConstructor.LobbyState(group));
        }

        public async Task JoinMarketLobby(string groupName)
        {
            if (groupName.Length != MarketCodeLength || !groupName.All(char.IsLetter)) 
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

            _logger.LogInformation($"Lobby {groupName} - user joined lobby"); 
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
            var oldState = marketService.State; 
            
            if (newState == oldState) return;
            
            switch (oldState)
            {
                case MarketState.Lobby:
                    if (newState == MarketState.Open) break;
                    throw new ArgumentException("Lobby state can only transition to Open");
                case MarketState.Open:
                    if (newState == MarketState.Paused) break;
                    if (newState == MarketState.Closed) break;
                    throw new ArgumentException("Open state can only transition to Paused or Closed");
                case MarketState.Paused:
                    if (newState == MarketState.Open) break;
                    if (newState == MarketState.Closed) break;
                    throw new ArgumentException("Paused state can only transition to Open or Closed");
                case MarketState.Closed:
                    if (newState == MarketState.Lobby) break;
                    throw new ArgumentException("Closed state can only transition to Lobby");
                default:
                    return;
            }

            marketService.State = newState;

            _logger.LogInformation($"Lobby {marketCode} - state updated to {marketService.State.ToString()}"); 
            await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
        }

        public async Task JoinMarket(string username)
        {
            var user = _userService.GetUser(Context.ConnectionId);
            if (username.Length == 0) throw new Exception("Username must be at least 1 character long");

            user.Name = username;
            
            var marketCode = user.Market;
            
            // TODO: retrieve cookie/local storage/claim etc

            _logger.LogInformation($"Lobby {marketCode} - {username} joined as participant"); 
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

            _logger.LogInformation($"Lobby {group} - order deleted"); 
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


            _logger.LogInformation($"Lobby {groupName} - order placed"); 
            await Clients.Group(groupName).NewOrder(_responseConstructor.NewOrder(originalOrder));
            
            var orderFilledTask = transactions
                .Select<Transaction, Task>(transaction => 
                    Clients.Group(groupName).TransactionEvent(_responseConstructor.Transaction(transaction)
                ));
            
            _logger.LogInformation($"Lobby {groupName} - {transactions.Count()} new transaction(s)"); 
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
                _logger.LogInformation($"Lobby {marketCode} - market closed");
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
            _logger.LogInformation($"Lobby {marketCode} - market closed with price");
            await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
            await Clients.Group(marketCode).ClosingPrices(closePrices);
        }

        public async Task Serialize()
        {
            var user = _userService.GetUser(Context.ConnectionId, admin:true);

            var marketService = _marketServices.Markets[user.Market];

            var json = JsonSerializer.Serialize(marketService);

            await Clients.Caller.ReceiveMessage(json);
        }
    }
}
