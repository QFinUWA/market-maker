using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {
        private readonly Dictionary<string, User> _users = new();
        private readonly Dictionary<string, List<User>> _groups = new();
        public void AddUser(string group, string id)
        {
            var newUser = new User
            {
                Market = group
            };
            _users[id] = newUser;
            
            if (!_groups.ContainsKey(group)) _groups.Add(group, new List<User>());
            _groups[group].Add(newUser);
            
        }

        public User? GetUser(string id)
        {
            return !_users.ContainsKey(id) ? null : _users[id];
        }

        public IEnumerable<User> GetUsers(string? gameCode = null)
        {
            if (gameCode == null) return _users.Values;

            return _groups.GetValueOrDefault(gameCode, new List<User>());
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
