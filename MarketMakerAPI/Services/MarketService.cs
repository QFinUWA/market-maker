using System.Text.Json.Serialization;
using MarketMaker.Contracts;
using MarketMaker.Models;

namespace MarketMaker.Services;

[Serializable]
public abstract class ExchangeService
{
    public abstract List<Order> Orders { get; set; }
    public abstract List<Transaction> Transactions { get; set; }
    public ExchangeConfig Config { get; set; } = new();

    public Dictionary<string, string> Users { get; } = new();

    public ExchangeState State { get; set; } = ExchangeState.Lobby;

    public int LobbySize { get; set; } = 0;

    [JsonIgnore] public List<string> Markets => Config.MarketNames.Keys.ToList();
    
    public string AddMarket()
    {
        var i = Markets.Count;
        var code = ((char)('A' + i % 26)).ToString();

        if (Config.MarketNames.ContainsKey(code)) throw new Exception("Maximum of 26 exchanges allowed");

        Config.MarketNames[code] = null;

        return code;
    }

    public void AddUser(string userId, string newUser)
    {
        if (Users.ContainsKey(newUser)) throw new ArgumentException();
        Users[userId] = newUser;
    }

    public bool UpdateConfig(ConfigUpdateRequest updateRequest)
    {
        if (updateRequest.MarketNames != null)
            if (updateRequest.MarketNames.Keys.Any(marketCode => !Markets.Contains(marketCode)))
                return false;

        Config.Update(updateRequest);
        return true;
    }

    public abstract List<Transaction>? NewOrder(Order newOrder);
    public abstract bool DeleteOrder(Guid id, string user);

    public abstract void Clear();
}