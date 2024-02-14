using System.Collections.Concurrent;
using System.Threading.Channels;
using MarketMaker.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketMaker.Services;

public class LocalExchangeService : ExchangeService
{
    protected readonly Dictionary<string, Market> Market = new();

    public override List<Transaction> Transactions { get; set; } = [];

    public override List<Order> Orders
    {
        get
        {
            List<Order> orders = new();

            foreach (var market in Market.Values) orders.AddRange(market.Orders);

            return orders;
        }
        set
        {
            foreach (var order in value)
            {
                Market.TryAdd(order.Market, new Market());
                Market[order.Market].InsertOrder(order);
                var side = order.Quantity > 0 ? Market[order.Market].Bid : Market[order.Market].Ask;
                side[order.Price].Enqueue(order.Id, order.TimeStamp);
            }
        }
    }

    protected override bool ProcessDeleteOrder(Order deleteOrder)
    {
        return Market[deleteOrder.Market].DeleteOrder(deleteOrder);
    }

    protected override (Order, List<Transaction>) ProcessNewOrder(NewOrderRequest newOrder)
    {
        return Market[newOrder.Market].NewOrder(newOrder);
    }
    
    protected override void ProcessClear()
    {
        Market.Clear();
        Orders.Clear();
        Transactions.Clear();
    }

    public override Order? GetOrder(Guid id)
    {
        return Market
            .Values
            .Select(market => market.GetOrder(id))
            .OfType<Order>()
            .SingleOrDefault();
    }

    protected override void AddMarket(string marketCode)
    {
        Market.TryAdd(marketCode, new Market());
    }
}