﻿using System.Text.Json;
using MarketMaker.Contracts;
using MarketMaker.Models;
using MarketMaker.Services;
using Microsoft.AspNetCore.SignalR;

namespace MarketMaker.Hubs;

public sealed class MarketHub : Hub<IExchangeClient>
{
    private const int ExchangeCodeLength = 5;
    private const int EmptyExchangeLifetimeMinutes = 60;
    private readonly Dictionary<string, CancellationTokenSource> _exchangeCancellationTokens;
    private readonly ExchangeGroup _exchangeServices;
    private readonly ILogger<MarketHub> _logger;
    private readonly Random _random;
    private readonly ResponseConstructor _responseConstructor;
    private readonly IUserService _userService;

    public MarketHub(
        ExchangeGroup exchangeServices,
        IUserService userService,
        Dictionary<string, CancellationTokenSource> cancellationTokens,
        ILogger<MarketHub> logger
    )
    {
        _exchangeServices = exchangeServices;
        _userService = userService;
        _random = new Random();
        _responseConstructor = new ResponseConstructor(_exchangeServices, _userService);
        _exchangeCancellationTokens = cancellationTokens;
        _logger = logger;
    }


    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User Connected");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        _logger.LogInformation("User Disconnected");
        User user;
        try
        {
            user = _userService.GetUser(Context.ConnectionId);
        }
        catch (Exception)
        {
            return;
        }

        user.Connected = false;

        var exchangeCode = user.ExchangeCode;

        if (_userService.GetUsers(exchangeCode).Any(u => u.Connected)) return;

        _logger.LogInformation($"Lobby {exchangeCode} empty - starting deletion countdown");
        var source = new CancellationTokenSource();

        _exchangeCancellationTokens[exchangeCode] = source;

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(EmptyExchangeLifetimeMinutes), source.Token);
            _exchangeServices.DeleteExchange(exchangeCode);
            _userService.DeleteUsers(exchangeCode);
        }
        catch (Exception)
        {
            _logger.LogInformation($"Lobby {exchangeCode} - countdown cancelled");
        }

        _logger.LogInformation($"Lobby {exchangeCode} - countdown complete: deleted");
        source.Dispose();

        _exchangeCancellationTokens.Remove(exchangeCode);
    }

    public async Task MakeNewExchange()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var stringChars = new char[ExchangeCodeLength];

        for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[_random.Next(chars.Length)];
        var exchangeCode = new string(stringChars);

        // create new exchange service

        _exchangeServices.Exchanges[exchangeCode] = new LocalExchangeService();

        // make user an admin
        _userService.AddAdmin(Context.ConnectionId, exchangeCode);

        _logger.LogInformation($"Added new exchange - {exchangeCode}");
        await Clients.Caller.LobbyState(_responseConstructor.LobbyState(exchangeCode));
        // await Clients.Caller.ExchangeState(_responseConstructor.ExchangeState(exchangeCode));
        await Groups.AddToGroupAsync(Context.ConnectionId, exchangeCode);
    }

    public async Task MakeNewMarket()
    {
        var user = _userService.GetUser(Context.ConnectionId, true);
        var exchangeCode = user.ExchangeCode;

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Lobby)
            throw new Exception("Cannot add Market while game in progress");

        // add new market
        var marketCode = exchangeService.AddMarket();

        // notify clients

        _logger.LogInformation($"Lobby {exchangeCode} - added market {marketCode}");
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
    }

    public async Task LoadExchange(string jsonSerialized)
    {
        var user = _userService.GetUser(Context.ConnectionId, true);
        var exchangeCode = user.ExchangeCode;

        var exchange = JsonSerializer.Deserialize<LocalExchangeService>(jsonSerialized);

        if (exchange == null) throw new Exception("Failed loading exchange.");
        _exchangeServices.Exchanges[exchangeCode] = exchange;

        _logger.LogInformation($"Loaded exchange from JSON - {exchangeCode}");

        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
        await Clients.Group(exchangeCode).ExchangeState(_responseConstructor.ExchangeState(exchangeCode));
    }

    public async Task Serialize()
    {
        var user = _userService.GetUser(Context.ConnectionId, true);

        var exchangeService = _exchangeServices.Exchanges[user.ExchangeCode];

        var json = JsonSerializer.Serialize(exchangeService);

        await Clients.Caller.ReceiveMessage(json);
    }

    public async Task UpdateConfig(ConfigUpdateRequest configUpdate)
    {
        var user = _userService.GetUser(Context.ConnectionId, true);

        var exchangeCode = user.ExchangeCode;

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Lobby)
            throw new Exception("Cannot update config while game in progress");

        exchangeService.UpdateConfig(configUpdate);

        _logger.LogInformation($"Lobby {exchangeCode} - updated config");
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
    }

    public async Task UpdateExchangeState(string newStateString)
    {
        var user = _userService.GetUser(Context.ConnectionId, true);

        var exchangeCode = user.ExchangeCode;

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

    public async Task CloseExchange(Dictionary<string, int>? closePrices = null)
    {
        var user = _userService.GetUser(Context.ConnectionId, true);

        var exchangeCode = user.ExchangeCode;

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

    public async Task JoinExchangeLobby(string exchangeCode)
    {
        if (exchangeCode.Length != ExchangeCodeLength || !exchangeCode.All(char.IsLetter))
            throw new Exception("Invalid exchange ID");

        var exchangeCodeUpper = exchangeCode.ToUpper();

        if (!_exchangeServices.Exchanges.ContainsKey(exchangeCodeUpper))
            throw new Exception("Group doesn't exist");

        var user = _userService.AddUser(exchangeCodeUpper, Context.ConnectionId);

        if (_exchangeCancellationTokens.ContainsKey(exchangeCode))
        {
            var token = _exchangeCancellationTokens[exchangeCode];
            token.Cancel();
            token.Dispose();
            _exchangeCancellationTokens.Remove(user.ExchangeCode);
        }

        _logger.LogInformation($"Lobby {exchangeCode} - user joined lobby");
        await Groups.AddToGroupAsync(Context.ConnectionId, exchangeCodeUpper);
        await Clients.Group(exchangeCode).LobbyState(_responseConstructor.LobbyState(exchangeCode));
        await Clients.Caller.ExchangeState(_responseConstructor.ExchangeState(exchangeCode));
    }

    public async Task JoinExchange(string username)
    {
        var user = _userService.GetUser(Context.ConnectionId);
        if (username.Length == 0) throw new Exception("Username must be at least 1 character long");

        user.Name = username;

        var exchangeCode = user.ExchangeCode;

        // TODO: retrieve cookie/local storage/claim etc

        _logger.LogInformation($"Lobby {exchangeCode} - {username} joined as participant");
        await Clients.Group(exchangeCode).NewParticipant(username);
    }

    public async Task PlaceOrder(string market, int price, int quantity)
    {
        if (price <= 0) throw new Exception("Price must be > 0");

        var user = _userService.GetUser(Context.ConnectionId);
        var username = user.Name;
        if (username == null) throw new Exception("You are not a participant");

        var exchangeCode = user.ExchangeCode;

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

        _logger.LogInformation($"Lobby {exchangeCode} - {transactions.Count()} new transaction(s)");
        await Task.WhenAll(orderFilledTask);
    }

    public async Task DeleteOrder(Guid orderId)
    {
        var user = _userService.GetUser(Context.ConnectionId);
        var exchangeCode = user.ExchangeCode;

        var username = user.Name;
        if (username == null) throw new Exception("You are not a participant");

        ExchangeService exchangeService = _exchangeServices.Exchanges[exchangeCode];

        if (exchangeService.State != ExchangeState.Open) throw new Exception("Exchange is not open");

        var orderDeleted = exchangeService.DeleteOrder(orderId, username);
        if (!orderDeleted) throw new Exception("Order deletion rejected");

        _logger.LogInformation($"Lobby {exchangeCode} - order deleted");
        await Clients.Group(exchangeCode).DeletedOrder(orderId);
    }
}