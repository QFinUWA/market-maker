namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {

        private Dictionary<string, Dictionary<string, string>> users = new();

        public void AddUser(string group, string id)
        {
            // TODO: make a modifiable struct
            users[id] = new Dictionary<string, string>
            {
                    { "market", group },
                    { "username", "" },
                    { "secret", "todo" },
                    {"admin", "false"}
            };
        }

        public Dictionary<string, string>? GetUser(string id)
        {
            if (!users.ContainsKey(id)) return null;
            
            return users[id];
        }

        public IEnumerable<Dictionary<string, string>> GetUsers()
        {
            return users.Values;
        }

        public void AddAdmin(string id, string marketCode)
        {
            
            users[id] = new Dictionary<string, string>
            {
                    { "market", marketCode },
                    { "username", "admin" },
                    { "secret", "todo" },
                    {"admin", "true "}
            };

        }

    }
}
