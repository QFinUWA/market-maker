using MarketMaker.Models;

namespace MarketMaker.Services
{
    public interface IUserService
    {
        void AddUser(string group, string id);

        User GetUser(string id, bool admin = false );

        IEnumerable<User> GetUsers(string? gameCode = null);
        void AddAdmin(string id, string marketCode);

    }
}
