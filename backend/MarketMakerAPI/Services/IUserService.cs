namespace MarketMaker.Services
{
    public interface IUserService
    {
        void AddUser(string id);
        Dictionary<string, string> Users { get; }
    }
}
