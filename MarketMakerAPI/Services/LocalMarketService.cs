using System.Collections.Concurrent;
using System.Threading.Channels;
using MarketMaker.Models;

namespace MarketMaker.Services;

public class LocalExchangeService : ExchangeService
{
    protected readonly Dictionary<string, Market> Market = new();
    
    public override List<Transaction> Transactions
    {
        get
        {
            List<Transaction> transactions = new();

            foreach (var market in Market.Values) transactions.AddRange(market.Transactions);

            return transactions;
        }
        set
        {
            foreach (var transaction in value)
            {
                Market.TryAdd(transaction.Market, new Market());
                Market[transaction.Market].Transactions.Add(transaction);
            }
        }
    }

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
                Market[order.Market].NewOrder(order);
            }
        }
    }

    protected override bool ProcessDeleteOrder(Order deleteOrder)
    {
        return Market[deleteOrder.Market].DeleteOrder(deleteOrder);
    }

    protected override List<Transaction>? ProcessNewOrder(Order newOrder)
    {
        return Market.GetValueOrDefault(newOrder.Market)?.NewOrder(newOrder);
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