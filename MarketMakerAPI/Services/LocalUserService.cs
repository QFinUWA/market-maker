using MarketMaker.Models;

namespace MarketMaker.Services;

public class LocalUserService : IUserService
{
    private readonly Dictionary<string, List<string>> _groups = new();
    private readonly Dictionary<string, User> _users = new();
    
    public User AddUser(string group, string id)
    {
        var newUser = new User(group);
        _users[id] = newUser;

        if (!_groups.ContainsKey(group)) _groups.Add(group, new List<string>());
        _groups[group].Add(id);

        return newUser;
    }

    public User GetUser(string id, bool admin = false)
    {
        if (!_users.ContainsKey(id)) throw new Exception("You are not a user");

        var user = _users[id];

        // only allow admin access
        if (admin && !user.IsAdmin) throw new Exception("You are not admin");

        return user;
    }
    
    // todo make a readonly property for efficiency (don't want to recreate this with every call)
    // todo: we don't really have much choice - either we have a O(1) retreival datastructure (which means O(n) every time we remove from the list, or O(n) retrieval but O(1) for adding)
    public IEnumerable<User> GetUsers(string? exchangeCode = null)
    {
        if (exchangeCode == null) return _users.Values;
        return _groups
            .GetValueOrDefault(exchangeCode, new List<string>())
            .Select(s => _users[s]);
    }

    // public User MakeUserAdmin(string id, string exchangeCode)
    // {
    //     var user = new User(exchangeCode)
    //     {
    //         IsAdmin = true
    //     };
    //     _users[id] = user;
    //     return user;
    // }

    public void DeleteUsers(string exchangeCode)
    {
        if (!_users.ContainsKey(exchangeCode)) return;
        if (!_groups.ContainsKey(exchangeCode)) return;

        _users.Remove(exchangeCode);
        _groups.Remove(exchangeCode);
    }

    public void UpdateId(string oldId, string newId)
    {
        var user = GetUser(oldId);
        _users.Remove(oldId);
        _users[newId] = user;
    }

    public bool IsUser(string id)
    {
        throw new NotImplementedException();
    }

    public void UpdateGroup(string id, string newGroup)
    {
        throw new NotImplementedException();
    }

    public bool IsUser(string id, string? exchangeCode = null)
    {
        var isUser = _users.ContainsKey(id);

        if (!isUser) return false;

        return exchangeCode is null || _users[id].ExchangeCode == exchangeCode;

    }
}