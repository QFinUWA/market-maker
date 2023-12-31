﻿using MarketMaker.Models;

namespace MarketMaker.Services;

public interface IUserService
{
    User AddUser(string group, string id);

    User GetUser(string id, bool admin = false);

    IEnumerable<User> GetUsers(string? exchangeCode = null);
    User AddAdmin(string id, string exchangeCode);

    void DeleteUsers(string exchangeCode);
}