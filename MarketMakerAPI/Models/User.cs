namespace MarketMaker.Models;

// TODO: make struct or record?
public class User
{
    private string? _name;

    public bool Connected = true;
    public bool IsAdmin = false;

    public User(string exchangeCode)
    {
        ExchangeCode = exchangeCode;
    }

    public string ExchangeCode { get; }

    public string? Name
    {
        get => _name;
        set => _name = value?.ToLower();
    }

    public string Secret { get; } = "";
}