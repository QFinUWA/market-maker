using MarketMaker.Models;

namespace MarketMaker.Services;

public interface IUserService
{
    User AddUser(string group, string id);

    User GetUser(string id, bool admin = false);

    IEnumerable<User> GetUsers(string? exchangeCode = null);
    // User MakeUserAdmin(string id, string exchangeCode);

    void DeleteUsers(string exchangeCode);

    void UpdateId(string oldId, string newId);

    bool IsUser(string id, string? group = null);

    void UpdateGroup(string id, string newGroup);
    
    
    // user doesn't exist           -> create new user
    // user is reconnecting         -> update connectionID
    // user is changing lobbies     -> add new user
}