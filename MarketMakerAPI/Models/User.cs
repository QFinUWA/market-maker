namespace MarketMaker.Models;

public class User
{
    public string Market { get; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set => _name = value?.ToLower();
    }

    public string Secret { get; } = "";
    public bool IsAdmin = false;

    public User(string market)
    {
        Market = market;
    }

}