namespace MarketMaker.Services;

public interface IUserDatabase
{
   bool CreateUser(string username, string password);

   bool ValidateUser(string username, string password);
}