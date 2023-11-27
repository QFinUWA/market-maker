namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {

        public Dictionary<string, Dictionary<string, string> > Users { get; }

        public Dictionary<string, string> Admins { get; }

        public LocalUserService() {
            Users = new();
            Admins = new();
        }


        public void AddUser(string group, string id)
        {
            Users[id] = new Dictionary<string, string>
            {
                { "market", group },
                { "username", "" },
                { "secret", "todo" },
            };
        }

        public void AddUsernameToUser(string id, string username)
        {
            Users[id]["username"] = username;
        }
    }
}
