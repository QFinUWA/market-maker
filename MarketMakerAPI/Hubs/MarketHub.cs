﻿using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMaker.Models;
using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs
{
    public sealed class MarketHub: Hub<IMarketClient>
    {
        private readonly MarketGroup _marketServices;
        private readonly IUserService _userServices;
        private const int MarketCodeLength = 5;
        private readonly Random _random;
        private readonly ResponseConstructor _responseConstructor;

        public MarketHub(MarketGroup marketServices, IUserService userService) 
        {
            _marketServices = marketServices;
            _userServices = userService;
            _random = new Random();
            _responseConstructor = new ResponseConstructor(_marketServices, _userServices);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
        
        public async Task MakeNewMarket()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringChars = new char[MarketCodeLength];
            
            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[_random.Next(chars.Length)];
            var marketCode = new String(stringChars);

            // create new market service
            var marketService = new LocalMarketService();
            _marketServices.Markets[marketCode] = marketService;

            // make user an admin
            _userServices.AddAdmin(Context.ConnectionId, marketCode);

            await Clients.Caller.LobbyState(_responseConstructor.LobbyState(marketCode));
            await Groups.AddToGroupAsync(Context.ConnectionId, marketCode);
        }

        public async Task MakeNewExchange()
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            if (user == null) throw new Exception("You are not a user");

            var group = user.Market;
            if (group == null) throw new Exception("You are not a market participant");

            // only allow admin access
            if (!user.IsAdmin) throw new Exception("You are not admin");

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
            var user = _userServices.GetUser(Context.ConnectionId);
            if (user == null) throw new Exception("You are not a user");
            
            var group = user.Market;
            if (group == null) throw new Exception("You are not a market participant");
            
            // only allow admin access
            if (!user.IsAdmin) throw new Exception("You are not admin");
            
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
            
            
            _userServices.AddUser(groupNameUpper, Context.ConnectionId);
            
            MarketService marketService = _marketServices.Markets[groupNameUpper];
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupNameUpper);
            await Clients.Group(groupName).LobbyState(_responseConstructor.LobbyState(groupName));
            await Clients.Caller.MarketState(_responseConstructor.MarketState(groupName));
        }
        
        public async Task UpdateMarketState(string newStateString)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            if (user == null) throw new Exception("You are not a user");
            
            var marketCode = user.Market;
            if (marketCode == null) throw new Exception("You are not a market participant");
            
            // only allow admin access
            if (!user.IsAdmin) throw new Exception("You are not admin");

            var stateExists = MarketState.TryParse(newStateString, true, out MarketState newState);
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
            var user = _userServices.GetUser(Context.ConnectionId);
            if (username.Length == 0) throw new Exception("Username must be at least 1 character long");
            
            if (user == null) throw new Exception("You are not a user");
            user.Name = username;
            
            var marketCode = user.Market;
            if (marketCode == null) throw new Exception("You are not a market participant");
            
            var marketService = _marketServices.Markets[marketCode];

            // retrieve cookie/local storage/claim etc
                        
            await Clients.Group(marketCode).NewParticipant(username);
        }

        public async Task DeleteOrder(Guid orderId)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            if (user?.Name == null) throw new Exception("You are not a user");
            
            var group = user.Market;
            if (group == null) throw new Exception("You are not a market participant");

            var username = user.Name;
            
            MarketService marketService = _marketServices.Markets[group];

            if (marketService.State != MarketState.Open) throw new Exception("Market is not open");
            
            var orderDeleted = marketService.DeleteOrder(orderId, username);
            if (!orderDeleted) throw new Exception("Order deletion rejected");
            
            await Clients.Group(group).DeletedOrder(orderId);
            
        }

        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            if (price <= 0) throw new Exception("Price must be > 0");
            
            var user = _userServices.GetUser(Context.ConnectionId);

            if (user?.Name == null) throw new Exception("You are not a user");

            if (user.Market == null) throw new Exception("You are not a market participant"); 
            
            var groupName = user.Market;
            
            MarketService marketService = _marketServices.Markets[groupName];
            if (marketService.State != MarketState.Open) throw new Exception("Market is not open");

            var newOrder = new Order(
                user.Name,
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
            
            var user = _userServices.GetUser(Context.ConnectionId);
            if (user == null) throw new Exception("You are not a user");
            
            var marketCode = user.Market;
            if (marketCode == null) throw new Exception("You are not a market participant");
            
            // only allow admin access
            if (!user.IsAdmin) throw new Exception("You are not admin");
            
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
