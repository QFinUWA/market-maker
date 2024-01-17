using System.Text.Json;
using MarketMaker.Contracts;
using MarketMaker.Models;
using MarketMaker.Services;
using Microsoft.AspNetCore.Authorization;

namespace MarketMaker.Hubs;

[Authorize]
public sealed class MarketHub : Microsoft.AspNetCore.SignalR.Hub<IExchangeClient>
{
    private const int EmptyExchangeLifetimeMinutes = 60;
    
    private readonly Dictionary<string, CancellationTokenSource> _exchangeCancellationTokens;
    private readonly ExchangeGroup _exchangeServices;
    private readonly ILogger<MarketHub> _logger;
    private readonly ResponseConstructor _responseConstructor;

    public const int ExchangeCodeLength = 5;

    public MarketHub(
        ExchangeGroup exchangeServices,
        Dictionary<string, CancellationTokenSource> cancellationTokens,
        ILogger<MarketHub> logger
    )
    {
        _exchangeServices = exchangeServices;
        _responseConstructor = new ResponseConstructor(_exchangeServices);
        _exchangeCancellationTokens = cancellationTokens;
        _logger = logger;
    }

    // TODO: move cancellation to new class
    private bool RemoveCancellationToken(string exchangeCode)
    {
        if (!_exchangeCancellationTokens.ContainsKey(exchangeCode)) return false;
        var token = _exchangeCancellationTokens[exchangeCode];
        token.Cancel();
        token.Dispose();
        _exchangeCancellationTokens.Remove(exchangeCode);
        return true;
    }

    private async Task CreateCancellationToken(string exchangeCode)
    {
        _logger.LogInformation($"Lobby {exchangeCode} empty - starting deletion countdown");
        var source = new CancellationTokenSource();

        _exchangeCancellationTokens[exchangeCode] = source;

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(EmptyExchangeLifetimeMinutes), source.Token);
            _exchangeServices.DeleteExchange(exchangeCode);
        }
        catch (Exception)
        {
            _logger.LogInformation($"Lobby {exchangeCode} - countdown cancelled");
        }

        _logger.LogInformation($"Lobby {exchangeCode} - countdown complete: deleted");
        source.Dispose();

        _exchangeCancellationTokens.Remove(exchangeCode);
    }
    
    public override async Task OnConnectedAsync()
    {
        var contextUser = Context.User!;
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");
        var isAdmin = CookieFactory.IsAdmin(contextUser);

        var exchangeExists = _exchangeServices.Exchanges.ContainsKey(exchangeCode);
        
        // create exchange 
        if (!exchangeExists)
        {
            if (!isAdmin) throw new UnauthorizedAccessException();
            
            _exchangeServices.Exchanges[exchangeCode] = new LocalExchangeService();
            _logger.LogInformation($"Added new exchange - {exchangeCode}");
        }

        _exchangeServices.Exchanges[exchangeCode].LobbySize++;
        RemoveCancellationToken(exchangeCode);

        _logger.LogInformation($"Lobby {exchangeCode} - user joined lobby");
        await Groups.AddToGroupAsync(Context.ConnectionId, exchangeCode);
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
        await Clients.Caller.ExchangeState(_responseConstructor.ExchangeState(exchangeCode));
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        _logger.LogInformation("User Disconnected");

        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");
        
        var exchange = _exchangeServices.Exchanges[exchangeCode];
        exchange.LobbySize--;
        
        if (exchange.LobbySize > 0) return;
        await CreateCancellationToken(exchangeCode);
    }
    
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task MakeNewMarket()
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");
        
        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Lobby)
            throw new Exception("Cannot add Market while game in progress");

        // add new market
        var marketCode = exchangeService.AddMarket();

        // notify clients
        _logger.LogInformation($"Lobby {exchangeCode} - added market {marketCode}");
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task LoadExchange(string jsonSerialized)
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");

        var exchange = JsonSerializer.Deserialize<LocalExchangeService>(jsonSerialized);

        if (exchange == null) throw new Exception("Failed loading exchange.");
        _exchangeServices.Exchanges[exchangeCode] = exchange;

        _logger.LogInformation($"Loaded exchange from JSON - {exchangeCode}");

        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
        await Clients.Group(exchangeCode).ExchangeState(_responseConstructor.ExchangeState(exchangeCode));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task Serialize()
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");

        var exchangeService = _exchangeServices.Exchanges[exchangeCode];

        var json = JsonSerializer.Serialize(exchangeService);

        await Clients.Caller.ReceiveMessage(json);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task UpdateConfig(ConfigUpdateRequest configUpdate)
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Lobby)
            throw new Exception("Cannot update config while game in progress");

        exchangeService.UpdateConfig(configUpdate);

        _logger.LogInformation($"Lobby {exchangeCode} - updated config");
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task UpdateExchangeState(string newStateString)
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");

        var stateExists = Enum.TryParse(newStateString, true, out ExchangeState newState);
        if (!stateExists) throw new Exception("Invalid state");

        var exchangeService = _exchangeServices.Exchanges[exchangeCode];
        var oldState = exchangeService.State;

        if (newState == oldState) return;

        switch (oldState)
        {
            case ExchangeState.Lobby:
                if (newState == ExchangeState.Open) break;
                throw new ArgumentException("Lobby state can only transition to Open");
            case ExchangeState.Open:
                if (newState == ExchangeState.Paused) break;
                if (newState == ExchangeState.Closed) break;
                throw new ArgumentException("Open state can only transition to Paused or Closed");
            case ExchangeState.Paused:
                if (newState == ExchangeState.Open) break;
                if (newState == ExchangeState.Closed) break;
                throw new ArgumentException("Paused state can only transition to Open or Closed");
            case ExchangeState.Closed:
                if (newState == ExchangeState.Lobby) break;
                throw new ArgumentException("Closed state can only transition to Lobby");
            default:
                return;
        }

        exchangeService.State = newState;

        _logger.LogInformation($"Lobby {exchangeCode} - state updated to {exchangeService.State.ToString()}");
        await Clients.Group(exchangeCode).StateUpdated(exchangeService.State.ToString());
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "admin")]
    public async Task CloseExchange(Dictionary<string, int>? closePrices = null)
    {
        var exchangeCode = CookieFactory.GetCookieValue(Context.User!, "exchangeCode");

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];
        if (exchangeService.State == ExchangeState.Lobby)
            throw new Exception("Exchange cannot be closed from the lobby");

        exchangeService.State = ExchangeState.Closed;

        if (closePrices == null)
        {
            exchangeService.Clear();
            _logger.LogInformation($"Lobby {exchangeCode} - exchange closed");
            await Clients.Group(exchangeCode).StateUpdated(exchangeService.State.ToString());
            return;
        }

        if (!(closePrices.Keys.All(exchangeService.Markets.Contains)
              && closePrices.Count == exchangeService.Markets.Count))
            throw new Exception("Incorrect market names");

        if (closePrices.Values.Any(price => price < 0))
            throw new Exception("Price must be positive");

        exchangeService.Clear();
        _logger.LogInformation($"Lobby {exchangeCode} - exchange closed with price");
        await Clients.Group(exchangeCode).StateUpdated(exchangeService.State.ToString());
        await Clients.Group(exchangeCode).ClosingPrices(closePrices);
    }

    public async Task JoinExchange(string username)
    {
        if (username.Length == 0) throw new Exception("Username must be at least 1 character long");
        var (userId, exchangeCode) = CookieFactory.GetUserAndGroup(Context.User!);
        var exchange = _exchangeServices.Exchanges[exchangeCode];
        if (exchange.Users.ContainsKey(username)) throw new ArgumentException($"Username \"{username}\" is taken");
        
        exchange.AddUser(userId, username);
        
        _logger.LogInformation($"Lobby {exchangeCode} - {username} joined as participant");
        await Clients.Group(exchangeCode).NewParticipant(username);
    }

    public async Task PlaceOrder(string market, int price, int quantity)
    {
        if (price <= 0) throw new Exception("Price must be > 0");
        var (userId, exchangeCode) = CookieFactory.GetUserAndGroup(Context.User!);
        var username = _exchangeServices.Exchanges[exchangeCode].Users[userId];
        
        if (username == null) throw new Exception("You are not a participant");

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];
        if (exchangeService.State != ExchangeState.Open) throw new Exception("Exchange is not open");

        var newOrder = new Order(
            username,
            market,
            price,
            quantity
        );

        var originalOrder = (Order)newOrder.Clone();

        var transactions = exchangeService.NewOrder(newOrder);
        if (transactions == null) throw new Exception("Invalid Order");

        _logger.LogInformation($"Lobby {exchangeCode} - order placed");
        await Clients.Group(exchangeCode).NewOrder(_responseConstructor.NewOrder(originalOrder));

        var orderFilledTask = transactions
            .Select<Transaction, Task>(transaction =>
                Clients.Group(exchangeCode).TransactionEvent(_responseConstructor.Transaction(transaction)
                ));

        _logger.LogInformation($"Lobby {exchangeCode} - {transactions.Count} new transaction(s)");
        await Task.WhenAll(orderFilledTask);
    }

    public async Task DeleteOrder(Guid orderId)
    {
        var (userId, exchangeCode) = CookieFactory.GetUserAndGroup(Context.User!);
        
        var username = _exchangeServices.Exchanges[exchangeCode].Users[userId];
        if (username == null) throw new Exception("You are not a participant");

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Open) throw new Exception("Exchange is not open");

        var orderDeleted = exchangeService.DeleteOrder(orderId, username);
        if (!orderDeleted) throw new Exception("Order deletion rejected");

        _logger.LogInformation($"Lobby {exchangeCode} - order deleted");
        await Clients.Group(exchangeCode).DeletedOrder(orderId);
    }
}