using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs
{
    public class EasyHub : Hub


    {

        public async Task Test(string message)
        {
            await Clients.Caller.SendAsync("RecieveMessage", message);
        }
    }
}
