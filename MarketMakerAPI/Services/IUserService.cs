using MarketMaker.Models;

namespace MarketMaker.Services
{
    public interface IUserService
    {
        void AddUser(string group, string id);

        User? GetUser(string id);

        IEnumerable<User> GetUsers(string? gameCode = null);
        void AddAdmin(string id, string marketCode);

    }
}
