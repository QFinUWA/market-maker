namespace MarketMaker.Models;

public class User
{
    public string? Market { get; set; } 
    public string? Name { get; set; }
    
    public string Secret;
    public bool IsAdmin = false;

    public User()
    {
        this.Secret = "";
    }
}