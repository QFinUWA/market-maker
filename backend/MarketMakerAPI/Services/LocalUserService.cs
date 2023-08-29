namespace MarketMaker.Services
{
    public class LocalUserService : IUserService
    {

        public Dictionary<string, string> Users { get; }

        public LocalUserService() {
            Users = new();        
        }


        public void AddUser(string id)
        {
            Users[id] = "";
        }
    }
}
