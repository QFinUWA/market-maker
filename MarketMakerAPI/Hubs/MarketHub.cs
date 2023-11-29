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
        private const int MarketCodeLength = 5;
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
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringChars = new char[MarketCodeLength];

            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[_random.Next(chars.Length)];

            var marketCode = new String(stringChars);
            
            // create new market service
            var marketService = new LocalMarketService();
            _marketService.Markets[marketCode] = marketService;

            // make user an admin
            _userServices.AddAdmin(Context.ConnectionId, marketCode);
            
            await Clients.Caller.ReceiveMessage($"added {marketCode} as a market.");
            await Clients.Caller.MarketConfig(MakeMarketConfigResponse(marketCode));
            await Groups.AddToGroupAsync(Context.ConnectionId, marketCode);
        }

        public async Task MakeNewExchange(string exchangeName)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var group = user.Market;

            // only allow admin access
            if (!user.IsAdmin) return;

            IMarketService marketService = _marketService.Markets[group];

            // add new exchange
            marketService.AddExchange(exchangeName);

            // notify clients
            // await Clients.Group(group).ReceiveMessage($"added {exchangeName} as an exchange.");
            // TODO: make marketconfigresponse take in the market service
            await Clients.Caller.MarketConfig(MakeMarketConfigResponse(group));
        }

        private MarketConfigResponse MakeMarketConfigResponse(string marketName)
        {
            IMarketService marketService = _marketService.Markets[marketName];

            return new MarketConfigResponse(marketName, marketService.Exchanges.ToList());
        }

        private MarketStateResponse MakeMarketStateResponse(string marketName)
        {
            IMarketService marketService = _marketService.Markets[marketName];
            return new MarketStateResponse(
                marketService.Participants,
                marketService.Orders,
                marketService.Transactions
            );
        }

        public async Task CloseMarket(Dictionary<string, int> prices)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var group = user.Market;

            // only allow admin access
            if (user.IsAdmin) return;

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
            
            await Clients.Caller.MarketConfig(MakeMarketConfigResponse(groupName));
            await Clients.Caller.MarketState(MakeMarketStateResponse(groupName));
        }
        
        public async Task JoinMarket(string username)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            
            // TODO: if not username or market return error
            
            user.Name = username;

            var marketService = _marketService.Markets[user.Market];

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
            
            IMarketService marketService = _marketService.Markets[group];

            marketService.DeleteOrder(orderId, username);

            await Clients.Group(group).DeletedOrder(orderId);
        }

        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            var user = _userServices.GetUser(Context.ConnectionId);
            var groupName = user.Market;

            if (user.Name == "") return; // TODO: make this more robust
            
            IMarketService marketService = _marketService.Markets[groupName];

            // TODO: NewOrder should raise an exception if the order was rejected, 
            //       in which case the clients shouldn't be alerted about a new order
            
            var (newOrder, transactions) = marketService.NewOrder(
                user.Name,
                exchange,
                price,
                quantity);


            await Clients.Group(groupName).NewOrder(new NewOrderResponse(
                newOrder.User,
                newOrder.Exchange,
                newOrder.Price,
                newOrder.Quantity,
                newOrder.TimeStamp,
                newOrder.Id
            ));
            

            var orderFilledTask = transactions.Select<TransactionEvent, Task>(transaction =>
                Clients.Group(groupName).TransactionEvent(new TransactionEventResponse(
                        transaction. BuyerUser,
                        transaction.BuyerOrderId,
                        transaction. SellerUser,
                        transaction.SellerOrderId,
                        transaction. Price,
                        transaction. Quantity,
                        transaction. Aggressor,
                        transaction. TimeStamp
                    ))
            );

            await Task.WhenAll(orderFilledTask);
        }
    }
}
