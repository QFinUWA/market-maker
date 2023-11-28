namespace MarketMaker.Services
{
    public interface IUserService
    {
        void AddUser(string group, string id);

        Dictionary<string, string>? GetUser(string id);

        IEnumerable<Dictionary<string, string>> GetUsers();

        void AddAdmin(string id, string marketCode);


    }
}
