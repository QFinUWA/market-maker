﻿using MarketMaker.Models;

namespace MarketMaker.Services;

public class LocalUserService : IUserService
{
    private readonly Dictionary<string, List<User>> _groups = new();
    private readonly Dictionary<string, User> _users = new();

    public User AddUser(string group, string id)
    {
        var newUser = new User(group);
        _users[id] = newUser;

        if (!_groups.ContainsKey(group)) _groups.Add(group, new List<User>());
        _groups[group].Add(newUser);

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

    public IEnumerable<User> GetUsers(string? exchangeCode = null)
    {
        if (exchangeCode == null) return _users.Values;

        return _groups.GetValueOrDefault(exchangeCode, new List<User>());
    }

    public User AddAdmin(string id, string exchangeCode)
    {
        var user = new User(exchangeCode)
        {
            IsAdmin = true
        };
        _users[id] = user;
        return user;
    }

    public void DeleteUsers(string exchangeCode)
    {
        if (!_users.ContainsKey(exchangeCode)) return;
        if (!_groups.ContainsKey(exchangeCode)) return;

        _users.Remove(exchangeCode);
        _groups.Remove(exchangeCode);
    }
}