using System.Net;
using System.Security.Claims;

namespace MarketMaker;

public abstract class CookieFactory
{
    public static string GetCookieValue(ClaimsPrincipal user, string key)
    {
        var identity = (ClaimsIdentity)user.Identity!;
        
        var value = identity.Claims.Where(c => c.Type == key)
            .Select(c => c.Value).Single();

        return value;
    }

    public static (string, string) GetUserAndGroup(ClaimsPrincipal user)
    {
        var identity = (ClaimsIdentity)user.Identity!;
        
        var userId = identity.Claims.Where(c => c.Type == "userId")
            .Select(c => c.Value).Single();

        var exchangeCode = identity.Claims.Where(c => c.Type == "exchangeCode")
            .Select(c => c.Value).Single();
        
        return (userId, exchangeCode);
    }

    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var identity = (ClaimsIdentity)user.Identity!;
        var value = identity.Claims.Where(c => c.Type == "admin")
            .Select(c => c.Value).SingleOrDefault();

        return value == "true";
    }
}