namespace MarketMaker.Services
{
    public class UserService
    {

        public Dictionary<string, string> Users { get; }

        public UserService() {
            Users = new();        
        }


        public void AddUser(string id)
        {
            Users[id] = "";
        }
    }
}
