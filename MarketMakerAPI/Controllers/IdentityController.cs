using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MarketMaker.Contracts;
using MarketMaker.Hubs;
using MarketMaker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MarketMaker.Controllers;

// [Route("api/[controller]")]
[ApiController]
public class IdentityController : Controller
{
   private readonly string _tokenSecret;
   private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(1);
   private readonly ExchangeGroup _exchanges;
   private readonly string _issuer;
   private readonly string _audience;
   
   public IdentityController(ExchangeGroup exchanges, IConfiguration config)
   {
      _exchanges = exchanges;
      _tokenSecret = config["AnonymousAccess"]!;
      _issuer = config["JwtSettings:Issuer"]!;
      _audience = config["JwtSettings:Audience"]!;
   }
   private string WriteToken(IEnumerable<Claim> claims)
   {
      JwtSecurityTokenHandler tokenHandler = new();

      byte[] key = Encoding.UTF8.GetBytes(_tokenSecret);
      SecurityTokenDescriptor tokenDescriptor = new()
      {
         Subject = new ClaimsIdentity(claims),
         Expires = DateTime.UtcNow.Add(TokenLifetime),
         Issuer = _issuer,
         Audience = _audience,
         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };

      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
   }

   // [HttpPost("create")]
   [HttpGet]
   [Route("create")]
   public IActionResult GenerateAdminToken()
   {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      var stringChars = new char[MarketHub.ExchangeCodeLength];
      Random random = new();
      for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[random.Next(chars.Length)];
      var exchangeCode = new string(stringChars);
      _exchanges.Exchanges[exchangeCode] = new LocalExchangeService();

      List<Claim> claims =
      [
         new Claim("userId", Guid.NewGuid().ToString()),
         new Claim("admin", "true"),
         new Claim("exchangeCode", exchangeCode)
      ];

      var jwt = WriteToken(claims);
      return Ok(jwt);
   }

   [HttpGet]
   [Route("join")]
   public IActionResult JoinMarket([FromQuery] string exchangeCode)
   {

      if (exchangeCode.Length != MarketHub.ExchangeCodeLength || !exchangeCode.All(char.IsLetter))
         return BadRequest($"Exchange code must be {MarketHub.ExchangeCodeLength} characters long.");

      var exchangeCodeUpper = exchangeCode.ToUpper();

      if (!_exchanges.Exchanges.ContainsKey(exchangeCodeUpper))
         return BadRequest($"Exchange {exchangeCodeUpper} does not exist.");

      var claims = new List<Claim>
      {
         new("userId", Guid.NewGuid().ToString()),
         new("admin", "false"),
         new("exchangeCode", exchangeCodeUpper)
      };

      var jwt = WriteToken(claims);
      return Ok(jwt);
   }

}
