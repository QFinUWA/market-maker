using MarketMaker.Models;

namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {
        private readonly Dictionary<string, User> _users = new();
        private readonly Dictionary<string, List<User>> _groups = new();
        public void AddUser(string group, string id)
        {
            var newUser = new User(group);
            _users[id] = newUser;
            
            if (!_groups.ContainsKey(group)) _groups.Add(group, new List<User>());
            _groups[group].Add(newUser);
            
        }

        public User GetUser(string id, bool admin=false)
        {
            if (!_users.ContainsKey(id)) throw new Exception("You are not a user");

            var user = _users[id];
            
            // only allow admin access
            if (admin && !user.IsAdmin) throw new Exception("You are not admin");

            return user;
        }

        public IEnumerable<User> GetUsers(string? gameCode = null)
        {
            if (gameCode == null) return _users.Values;

            return _groups.GetValueOrDefault(gameCode, new List<User>());
        }

        public void AddAdmin(string id, string marketCode)
        {
            var user = new User(marketCode)
            {
                IsAdmin = true
            };
            _users[id] = user;
        }

    }
}
