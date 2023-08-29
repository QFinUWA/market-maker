using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMaker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs
{
    public sealed class MarketHub: Hub<IMarketClient>
    {
        private readonly IMarketService _marketService;
        private readonly IUserService _userService;

        public MarketHub(IMarketService marketService, IUserService userService) 
        { 
            _marketService = marketService;
            _userService = userService;
        }

        public async Task Test(string message)
        {
            await Clients.Caller.RecieveMessage(message);
        }


        public override async Task OnConnectedAsync()
        {
            _userService.AddUser(Context.ConnectionId);

            await Clients.Others.UserJoined(Context.ConnectionId);
            await Clients.Caller.MarketState(new MarketStateResponse(_userService.Users.Keys.ToList(), _marketService.GetOrders()));
        }

        public async Task PlaceOrder(string market, int price, int quantity)
        {

            Order order = Order.MakeOrder(
                Context.ConnectionId,
                market,
                price,
                quantity);

            List<Order> filledOrders = _marketService.NewOrder(order);

            await Clients.All.NewOrder(new NewOrderResponse(
                    order.User,
                    order.Market,
                    order.Price,
                    order.Quantity,
                    order.CreatedAt,
                    order.Id
                ));


            foreach (var filledOrder in filledOrders)
            {
                await Clients.All.OrderFilled(new OrderFilledResponse(filledOrder.Market, filledOrder.Id, filledOrder.Quantity));
            }

        }

        public async Task DeleteOrder(DeleteOrderRequest deleteOrderRequest)
        {
            var market = deleteOrderRequest.market;
            var id = deleteOrderRequest.Id;

            _marketService.DeleteOrder(market, id);

            await Clients.All.DeletedOrder(new DeleteOrderResponse(market, id));
        }

        //public async Task AmmendOrder(Guid id, NewOrderRequest orderRequest)
        //{
        //    Order order = Order.MakeOrder(
        //        Context.ConnectionId,
        //        orderRequest.Market,
        //        orderRequest.Price,
        //        orderRequest.Quantity,
        //        id
        //        );

        //    Order oldOrder = _marketService.UpdateOrder(order);

        //    await Clients.All.DeletedOrder(oldOrder.Id);

        //    await Clients.All.AmmendedOrder(new NewOrderResponse(
        //       order.User,
        //       order.Market,
        //       order.Price,
        //       order.Quantity,
        //       order.CreatedAt,
        //       order.Id
        //       ));
        //}



    }
}
