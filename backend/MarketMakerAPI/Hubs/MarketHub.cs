using MarketMaker.Contracts;
using MarketMaker.Services;
using MarketMakerAPI.Contracts;
using MarketMakerAPI.Models;
using MarketMakerAPI.Services;
using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs
{
    public sealed class MarketHub: Hub<IMarketClient>
    {
        private readonly LocalMarketService _marketService = new();
        private readonly static UserService _userService = new();


        public override async Task OnConnectedAsync()
        {
            _userService.AddUser(Context.ConnectionId);

            await Clients.Others.UserJoined(Context.ConnectionId);
            await Clients.Caller.MarketState(new MarketStateResponse(_userService.Users.Keys.ToList(), _marketService.Orders));
        }

        public async Task PlaceOrder(NewOrderRequest orderRequest)
        {
            Order order = Order.MakeOrder(
                Context.ConnectionId,
                orderRequest.Market,
                orderRequest.Price,
                orderRequest.Quantity);

            _marketService.NewOrder(order);

            await Clients.All.NewOrder(new NewOrderResponse(
                    order.User,
                    order.Market,
                    order.Price,
                    order.Quantity,
                    order.CreatedAt,
                    order.Id
                ));
        }

        public async Task DeleteOrder(Guid orderId)
        {
            _marketService.DeleteOrder(orderId);

            await Clients.All.DeletedOrder(orderId);
        }

        public async Task AmmendOrder(Guid id, NewOrderRequest orderRequest)
        {
            Order order = Order.MakeOrder(
                Context.ConnectionId,
                orderRequest.Market,
                orderRequest.Price,
                orderRequest.Quantity,
                id
                );

            Order oldOrder = _marketService.UpdateOrder(order);

            await Clients.All.DeletedOrder(oldOrder.Id);

            await Clients.All.AmmendedOrder(new NewOrderResponse(
               order.User,
               order.Market,
               order.Price,
               order.Quantity,
               order.CreatedAt,
               order.Id
               ));
        }



    }
}
