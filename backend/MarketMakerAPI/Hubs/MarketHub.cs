using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMaker.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
//using Microsoft.AspNet.SignalR;

namespace MarketMaker.Hubs
{
    public sealed class MarketHub: Hub<IMarketClient>
    {
        private readonly MarketGroup _marketService;
        private readonly IUserService _userServices;
        private readonly int _marketcodelength = 5;
        private readonly Random _random;
        public MarketHub(MarketGroup marketService, IUserService userService) 
        {
            _marketService = marketService;
            _userServices = userService;
            _random = new Random();
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
        public async Task MakeNewMarket()
        {
            var  chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringChars = new char[_marketcodelength];

            for (int i = 0; i < stringChars.Length; i++) stringChars[i] = chars[_random.Next(chars.Length)];

            var marketCode = new String(stringChars);
            
            // create new market service
            var marketService = new LocalMarketService();
            _marketService.Markets[marketCode] = marketService;

            await JoinMarketLobby(marketCode);
            
            // make user an admin
            _userServices.Admins[marketCode] = Context.ConnectionId;
            
            await Clients.Caller.ReceiveMessage($"added {marketCode} as a market.");
            await Clients.Caller.MarketState(new MarketStateResponse(_userServices.Users.Keys.ToList(), marketService.GetOrders(), marketCode, marketService.Exchanges));
            await Groups.AddToGroupAsync(Context.ConnectionId, marketCode);
        }

        public async Task MakeNewExchange(string exchangeName)
        {
            var userProfile = _userServices.Users[Context.ConnectionId];
            var group = userProfile["market"];

            // only allow admin access
            if (_userServices.Admins[group] != Context.ConnectionId) return;

            IMarketService marketService = _marketService.Markets[group];

            // add new exchange
            marketService.AddExchange(exchangeName);

            // notify clients
            await Clients.Group(group).ReceiveMessage($"added {exchangeName} as an exchange.");
            await Clients.Group(group).ExchangeAdded(exchangeName);
        }

        public async Task CloseMarket(Dictionary<string, int> prices)
        {
            var userProfile = _userServices.Users[Context.ConnectionId];
            var group = userProfile["market"];

            // only allow admin access
            if (_userServices.Admins[group] != Context.ConnectionId) return;

            IMarketService marketService = _marketService.Markets[group];

            var profits = marketService.CloseMarket(prices);

            // await Clients.Group(group).UpdateGameState("paused");
            //TODO: possibly don't make the new exchanges until the game has started by the admin - add a "market open"
            // flag in the market which prevents activity but have the state be clientside
            // await Clients.Group(group).
        }

        public async Task JoinMarketLobby(string groupName)
        {
            groupName = groupName.ToUpper();
            
            IMarketService marketService = _marketService.Markets[groupName];

            _userServices.AddUser(groupName, Context.ConnectionId);
            
            await Clients.Caller.ReceiveMessage($"joined lobby");
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.MarketState(new MarketStateResponse(_userServices.Users.Keys.ToList(),
                marketService.GetOrders(), groupName, marketService.Exchanges));
        }
        
        public async Task JoinMarket(string username)
        {

            // retrieve cookie/local storage/claim etc
            var userNames = _userServices.Users
                .Values
                .Select(user => user["username"].ToLower())
                .ToList();
            
            if (userNames.Contains(username.ToLower()))
            {
                // TODO: Add proper error handling 
                // TODO: Add reconnection authorisation
                return;
            }
            
            var userProfile = _userServices.Users[Context.ConnectionId];
            userProfile["username"] = username;
            await Clients.Caller.ReceiveMessage($"joined market"); 
            await Clients.Group(userProfile["market"]).UserJoined(username);
        }


        public async Task DeleteOrder(Guid orderId)
        {
            var userProfile = _userServices.Users[Context.ConnectionId];
            var group = userProfile["market"];
            
            if (userProfile["username"] == "") return; // TODO: make this more robust
            
            IMarketService marketService = _marketService.Markets[group];

            marketService.DeleteOrder(orderId);

            await Clients.Group(group).DeletedOrder(orderId);
        }

        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            var userProfile = _userServices.Users[Context.ConnectionId];
            var groupName = userProfile["market"];

            if (userProfile["username"] == "") return; // TODO: make this more robust
            
            IMarketService marketService = _marketService.Markets[groupName];

            var order = Order.MakeOrder(
                userProfile["username"],
                exchange,
                price,
                quantity);
            var originalOrder = (Order)order.Clone();

            // TODO: NewOrder should raise an exception if the order was rejected, 
            //       in which case the clients shouldn't be alerted about a new order

            var filledOrders = marketService.NewOrder(order);


            await Clients.Group(groupName).NewOrder(new NewOrderResponse(
                originalOrder.User,
                originalOrder.Exchange,
                originalOrder.Price,
                originalOrder.Quantity,
                originalOrder.TimeStamp,
                originalOrder.Id
            ));

            var orderFilledTask = filledOrders.Select<TransactionEvent, Task>(filledOrder =>
                Clients.Group(groupName).TransactionEvent(filledOrder)
            );

            await Task.WhenAll(orderFilledTask);
        }
    }
}
