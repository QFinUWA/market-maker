using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {

        private readonly Dictionary<string, User> _users = new();

        public void AddUser(string group, string id)
        {
            _users[id] = new User
            {
                Market = group
            };
            
        }

        public User? GetUser(string id)
        {
            if (!_users.ContainsKey(id)) return null;
            
            return _users[id];
        }

        public IEnumerable<User> GetUsers()
        {
            return _users.Values;
        }

        public void AddAdmin(string id, string marketCode)
        {
            _users[id] = new User
            {
                Market = marketCode,
                IsAdmin = true,
            };
        }

    }
}
