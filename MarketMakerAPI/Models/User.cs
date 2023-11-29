namespace MarketMaker.Models;

public class User
{
    public string? Market { get; set; } 
    public string? Name { get; set; }
    
    public string Secret { get; } = "";
    public bool IsAdmin = false;

}