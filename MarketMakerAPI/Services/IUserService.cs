namespace MarketMaker.Services
{
    public interface IUserService
    {
        void AddUser(string group, string id);

        Dictionary<string, Dictionary<string, string>> Users { get; }
        Dictionary<string, string> Admins { get; } // group -> adminId
    }
}
