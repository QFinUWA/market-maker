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

        public MarketHub(MarketGroup marketServices, IUserService userService) 
        {
            _marketServices = marketServices;
            _userServices = userService;
            _random = new Random();
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
            await Clients.Caller.MarketCreated(marketCode);
            await Clients.Caller.StateUpdated(MarketState.Lobby.ToString());
            await Groups.AddToGroupAsync(Context.ConnectionId, marketCode);
        }

        public async Task MakeNewExchange(string exchangeName)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var group = user.Market;

            // only allow admin access
            if (!user.IsAdmin) return;

            MarketService marketService = _marketServices.Markets[group];

            // add new exchange
            try
            {
                marketService.AddExchange(exchangeName);
            }
            catch (InvalidOperationException e)
            {
                
            }

            // notify clients
            // await Clients.Group(group).ReceiveMessage($"added {exchangeName} as an exchange.");
            // TODO: make marketconfigresponse take in the market service
            await Clients.Group(group).MarketConfig(ResponseConstructor.MarketConfig(marketService));

        }

        public async Task CloseMarket(Dictionary<string, int> prices)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var group = user.Market;

            // only allow admin access
            if (!user.IsAdmin) return;

            MarketService marketService = _marketServices.Markets[group];
            Dictionary<string, float> profits;
            try
            {
                profits = marketService.CloseMarket(prices);
            }
            catch (InvalidOperationException e)
            {
                
            }

            // await Clients.Group(group).UpdateGameState("paused");
            //TODO: possibly don't make the new exchanges until the game has started by the admin - add a "market open"
            // flag in the market which prevents activity but have the state be clientside
            // await Clients.Group(group).
        }

        public async Task JoinMarketLobby(string groupName)
        {
            groupName = groupName.ToUpper();
            
            MarketService marketService = _marketServices.Markets[groupName];

            _userServices.AddUser(groupName, Context.ConnectionId);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            await Clients.Caller.MarketConfig(ResponseConstructor.MarketConfig(marketService));
            await Clients.Caller.MarketState(ResponseConstructor.MarketState(marketService));

        }
        
        public async Task UpdateMarketState(string newStateString)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var marketCode = user.Market;
            
            // only allow admin access
            if (!user.IsAdmin) return;

            bool stateExists = MarketState.TryParse(newStateString, true, out MarketState newState);

            if (!stateExists) return;
                
            var marketService = _marketServices.Markets[marketCode];
            var currState = marketService.State;
            
            /*
             * inLobby -> (open)
             * open -> (paused, closed)
             * paused -> (open, closed)
             * closed -> (open)
             */
            switch (currState)
            {
                case MarketState.Lobby:
                    if (newState == MarketState.Open) break;
                    return;
                case MarketState.Open:
                    if (newState == MarketState.Paused) break;
                    if (newState == MarketState.Closed) break;
                    return;
                case MarketState.Paused:
                    if (newState == MarketState.Open) break;
                    if (newState == MarketState.Closed) break;
                    return;
                case MarketState.Closed:
                    if (newState == MarketState.Open) break;
                    return;
                default:
                    return;
            }

            marketService.State = newState;
            
            await Clients.Group(marketCode).StateUpdated(marketService.State.ToString());
        }

        public async Task JoinMarket(string username)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            
            // TODO: if not username or market return error
            
            user.Name = username;

            var marketService = _marketServices.Markets[user.Market];

            // retrieve cookie/local storage/claim etc
            
            marketService.AddParticipant(username);
            
            await Clients.Caller.ReceiveMessage($"joined market"); 
            await Clients.Group(user.Market).UserJoined(username);
        }


        public async Task DeleteOrder(Guid orderId)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var group = user.Market;
            var username = user.Name;
            
            if (username == "") return; // TODO: make this more robust
            
            MarketService marketService = _marketServices.Markets[group];

            marketService.DeleteOrder(orderId, username);

            await Clients.Group(group).DeletedOrder(orderId);
        }

        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var groupName = user.Market;

            if (user.Name == "") return; // TODO: make this more robust
            
            MarketService marketService = _marketServices.Markets[groupName];

            // TODO: NewOrder should raise an exception if the order was rejected, 
            //       in which case the clients shouldn't be alerted about a new order

            Order newOrder;
            List<Transaction> transactions;
            try
            {
                (newOrder, transactions) = marketService.NewOrder(
                    user.Name,
                    exchange,
                    price,
                    quantity);
            }
            catch (InvalidOperationException e)
            {
                throw e;
            }


            await Clients.Group(groupName).NewOrder(ResponseConstructor.NewOrder(newOrder));
            

            var orderFilledTask = transactions.Select<Transaction, Task>(transaction =>
                Clients.Group(groupName).TransactionEvent(ResponseConstructor.Transaction(transaction))
            );

            await Task.WhenAll(orderFilledTask);
        }
    }
}
