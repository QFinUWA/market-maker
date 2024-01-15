using System.ComponentModel.DataAnnotations;

namespace MarketMaker.Services;

public class LocalUserDatabase : IUserDatabase
{
    private readonly Dictionary<string, string> _db = new();
    
    public bool CreateUser(string email, string password)
    {
        if (_db.ContainsKey(email)) return false;

        _db[email] = password;
        return true;
    }

    public bool ValidateUser(string email, string password)
    {
        if (!_db.ContainsKey(email)) throw new ValidationException("User does not exist");

        return _db[email] == password;
    }
}