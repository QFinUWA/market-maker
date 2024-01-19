using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using MarketMaker.Contracts;
using MarketMaker.Hubs;
using MarketMaker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MarketMaker.Controllers;

[ApiController]
public class IdentityController(ExchangeGroup exchanges, IConfiguration config, UserDbContext dbContext)
   : Controller
{
   private readonly string _tokenSecret = config["AnonymousAccess"]!;
   private static readonly TimeSpan AnonymousTokenLifetime = TimeSpan.FromDays(1);
   private static readonly TimeSpan RegisteredTokenLifetime = TimeSpan.FromDays(28);
   private readonly string _issuer = config["JwtSettings:Issuer"]!;
   private readonly string _audience = config["JwtSettings:Audience"]!;

   private string WriteToken(IEnumerable<Claim> claims, bool authenticated = false)
   {
      var time = authenticated ? RegisteredTokenLifetime : AnonymousTokenLifetime;
      
      JwtSecurityTokenHandler tokenHandler = new();

      byte[] key = Encoding.UTF8.GetBytes(_tokenSecret);
      SecurityTokenDescriptor tokenDescriptor = new()
      {
         Subject = new ClaimsIdentity(claims),
         Expires = DateTime.UtcNow.Add(time),
         Issuer = _issuer,
         Audience = _audience,
         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };

      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
   }
   
   private static bool IsValidEmail(string email)
   {
      const string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
		
      if (string.IsNullOrEmpty(email))
         return false;
		
      var regex = new Regex(emailPattern);
      return regex.IsMatch(email);
   }
   
   private static (string userId, bool authenticated) GetStoredIdOrNew(ClaimsPrincipal contextUser)
   {
      var identity = (ClaimsIdentity)contextUser.Identity!;
      var authenticated = identity.Claims.Where(c => c.Type == "authenticatedUser")
         .Select(c => c.Value).FirstOrDefault("false");
      var userId = identity.Claims.Where(c => c.Type == "userId")
         .Select(c => c.Value).FirstOrDefault(Guid.NewGuid().ToString());

      return (userId, bool.Parse(authenticated));
   }

   [HttpPost]
   [Route("login")]
   public IActionResult LoginUser(LoginRequest request)
   {
      var email = request.Email;
      // var users = dbContext.User.Select(e => e.Email).ToList();
      var user = dbContext.User.FirstOrDefault(user => user.Email == email);
      if (user is null) return Forbid();
   
      var storedHash = user.PasswordHash;
      if (!PasswordHasher.Verify(storedHash, request.Password))
         return Forbid();

      List<Claim> claims =
      [
         new Claim("userId", user.UserId.ToString()),
         new Claim("authenticatedUser", "true"),
      ];

      var jwt = WriteToken(claims, authenticated: true);
      return Ok(jwt);
   }
   
   [HttpPost]
   [Route("createAccount")]
   public IActionResult CreateUser(CreateUserRequest request)
   {
      return BadRequest("this function is disable (for development)");
      var email = request.Email;

      if (!IsValidEmail(email)) return BadRequest("Invalid email format.");
      
      var existingUser = dbContext.User.FirstOrDefault(user => user.Email == email);
      if (existingUser is not null) return BadRequest("Email already exists.");
      
      var password = request.Password;
      var passwordHash = PasswordHasher.Hash(password);

      User newUser = new();
      newUser.Email = email;
      newUser.PasswordHash = passwordHash;
      newUser.UserId = Guid.NewGuid();
      dbContext.User.Add(newUser);
      try
      {
         dbContext.SaveChanges();
      }
      catch
      {
         return StatusCode(StatusCodes.Status500InternalServerError);
      }
      
      return Ok();
   }

   [HttpGet]
   [Route("createExchange")]
   [Authorize(Policy = "authenticatedUser")] // only our single account can access this method for now
   public IActionResult CreateExchange()
   {
      string exchangeCode = exchanges.AddExchange();
      var (userId, authenticated) = GetStoredIdOrNew(HttpContext.User);
      
      List<Claim> claims =
      [
         new Claim("userId", userId),
         new Claim("authenticatedUser", authenticated.ToString()),
         new Claim("exchangeCode", exchangeCode),
         new Claim("admin", "true")
      ];

      var jwt = WriteToken(claims, authenticated: authenticated);
      return Ok(jwt);
   }

   [HttpGet]
   [Route("joinExchange")]
   public IActionResult JoinExchange([FromQuery] string exchangeCode)
   {

      if (exchangeCode.Length != ExchangeHub.ExchangeCodeLength || !exchangeCode.All(char.IsLetter))
         return BadRequest($"Exchange code must be {ExchangeHub.ExchangeCodeLength} characters long.");

      var exchangeCodeUpper = exchangeCode.ToUpper();

      if (!exchanges.Exchanges.ContainsKey(exchangeCodeUpper))
         return BadRequest($"Exchange {exchangeCodeUpper} does not exist.");

      var (userId, authenticated) = GetStoredIdOrNew(HttpContext.User);
      
      List<Claim> claims =
      [
         new Claim("userId", userId),
         new Claim("authenticatedUser", authenticated.ToString()),
         new Claim("exchangeCode", exchangeCode),
         new Claim("admin", "false")
      ];

      var jwt = WriteToken(claims, authenticated: authenticated);
      return Ok(jwt);
   }

}
