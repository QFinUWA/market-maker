namespace MarketMaker.Models;

public class User
{
    public string ExchangeCode { get; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set => _name = value?.ToLower();
    }

    public bool Connected = true;
    public string Secret { get; } = "";
    public bool IsAdmin = false;

    public User(string exchangeCode)
    {
        ExchangeCode = exchangeCode;
    }

}