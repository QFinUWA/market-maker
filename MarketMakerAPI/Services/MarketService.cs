using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services;


public interface IRequest;

public record NewOrderRequest(Order Order) : IRequest;

public record DeleteOrderRequest(Order Order) : IRequest;

public record ClearRequest : IRequest;

[Serializable]
public abstract class ExchangeService
{
    public abstract List<Order> Orders { get; set; }
    public abstract List<Transaction> Transactions { get; set; }
    public ExchangeConfig Config { get; set; } = new();
    private object _lock = new();

    private Channel<IRequest> _orderQueue = Channel.CreateUnbounded<IRequest>();
    private static ConcurrentQueue<List<Transaction>?> _transactionQueue = new();
    private BlockingCollection<List<Transaction>?> _transactions = new(collection: _transactionQueue);

    private CancellationTokenSource _cancellationTokenSource = new();
    public async void Listen()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var message = await _orderQueue.Reader.ReadAsync(_cancellationTokenSource.Token);
            switch (message)
            {
                case NewOrderRequest newOrderRequest:
                    var transactions = ProcessNewOrder(newOrderRequest.Order);
                    _transactions.Add(transactions);
                    
                    break;
                case DeleteOrderRequest deleteOrderRequest:
                    ProcessDeleteOrder(deleteOrderRequest.Order);
                    break;
                case ClearRequest:
                    ProcessClear();
                    break;
            }
        }
    }

    public void StopListening()
    {
       _cancellationTokenSource.Cancel();
    }
    public ConcurrentDictionary<string, string> Users { get; } = new();

    public ExchangeState State { get; set; } = ExchangeState.Lobby;

    public int LobbySize { get; set; } = 0;

    [JsonIgnore] public Dictionary<string, string?> Markets => Config.MarketNames;
    
    public string NewMarket()
    {
        var i = Markets.Count;
        var code = ((char)('A' + i % 26)).ToString();

        if (Config.MarketNames.ContainsKey(code)) throw new Exception("Maximum of 26 exchanges allowed");
        lock (_lock) Config.MarketNames[code] = null;
        AddMarket(code);
        return code;
    }

    public bool AddUser(string userId, string username)
    {
        if (Users.Values.Contains(username)) return false;
        lock (_lock) Users[userId] = username;
        return true;
    }

    public bool UpdateConfig(ConfigUpdateRequest updateRequest)
    {
        if (updateRequest.MarketNames != null)
            if (updateRequest.MarketNames.Keys.Any(marketCode => !Markets.ContainsKey(marketCode)))
                return false;
        lock(_lock) Config.Update(updateRequest);
        return true;
    }

    public async Task NewOrder(Order order)
    {
        await _orderQueue.Writer.WriteAsync(new NewOrderRequest(order));
    }

    public async Task DeleteOrder(Order deleteOrder)
    {
        await _orderQueue.Writer.WriteAsync(new DeleteOrderRequest(deleteOrder));
    }

    public async Task Clear()
    {
        await _orderQueue.Writer.WriteAsync(new ClearRequest());
    }

    protected abstract List<Transaction>? ProcessNewOrder(Order newOrder);
    protected abstract bool ProcessDeleteOrder(Order deleteOrder);
    protected abstract void ProcessClear();
    public abstract Order? GetOrder(Guid id);
    protected abstract void AddMarket(string marketCode);

    public List<Transaction>? GetNewTransactions()
    {
        return _transactions.Take();
    }
}