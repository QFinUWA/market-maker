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


        public MarketHub(MarketGroup marketService, IUserService userService) 
        {
            _marketService = marketService;
            _userServices = userService;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.GetHttpContext()?.Request.Query["username"];

            await base.OnConnectedAsync();
        }
        public async Task MakeNewMarket(string marketName)
        {
            if (_marketService.Markets.ContainsKey(marketName)) return;
            _userServices.Users[Context.ConnectionId] = marketName;
            _userServices.Admins[marketName] = Context.ConnectionId;
            _marketService.Markets[marketName] = new LocalMarketService();

            await Clients.Caller.RecieveMessage($"Successfully added {marketName} as a market.");
            await Groups.AddToGroupAsync(Context.ConnectionId, marketName);
        }

        public async Task MakeNewExchange(string exchangeName)
        {
            string group = _userServices.Users[Context.ConnectionId];

            if (_userServices.Admins[group] != Context.ConnectionId) return;

            IMarketService marketService = _marketService.Markets[group];

            marketService.AddExchange(exchangeName);
            await Clients.Group(group).RecieveMessage($"Successfully added {exchangeName} as an exchange.");
            await Clients.Group(group).ExchangeAdded(exchangeName);
        }

        public async Task JoinMarket(string groupName)
        {
            var marketService = _marketService.Markets[groupName];
            _userServices.AddUser(groupName, Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
      
            await Clients.Group(groupName).UserJoined(Context.ConnectionId);
            await Clients.Caller.MarketState(new MarketStateResponse(_userServices.Users.Keys.ToList(), marketService.GetOrders(), marketService.Exchanges));
        }



        public async Task PlaceOrder(string exchange, int price, int quantity)
        {
            IMarketService marketService = _marketService.Markets[_userServices.Users[Context.ConnectionId]];
            string groupName = _userServices.Users[Context.ConnectionId];

            Order order = Order.MakeOrder(
                Context.ConnectionId,
                exchange,
                price,
                quantity);

            Order originalOrder = (Order)order.Clone();

            // TODO: NewOrder should raise an exception if the order was rejected, 
            //       in which case the clients shouldn't be alerted about a new order

            List<Order> filledOrders = marketService.NewOrder(order);

            await Clients.Group(groupName).NewOrder(new NewOrderResponse(
                    originalOrder.User,
                    originalOrder.Exchange,
                    originalOrder.Price,
                    originalOrder.Quantity,
                    originalOrder.CreatedAt,
                    originalOrder.Id
                ));


            foreach (Order filledOrder in filledOrders)
            {
                await Clients.Group(groupName).OrderFilled(new OrderFilledResponse(
                    filledOrder.Exchange, 
                    filledOrder.Id, 
                    filledOrder.User, 
                    filledOrder.Price, 
                    filledOrder.Quantity));
            }

            

        }

        public async Task DeleteOrder(DeleteOrderRequest deleteOrderRequest)
        {
            string group = _userServices.Users[Context.ConnectionId];
            IMarketService marketService = _marketService.Markets[group];

            var exchange = deleteOrderRequest.exchange;
            var id = deleteOrderRequest.Id;

            marketService.DeleteOrder(exchange, id);

            await Clients.Group(group).DeletedOrder(new DeleteOrderResponse(exchange, id));
        }
    }
}
