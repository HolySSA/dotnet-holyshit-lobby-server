using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace HolyShitServer.Src.Services;

public class TokenValidationService
{
  private readonly IConfiguration _config;
  private readonly IConnectionMultiplexer _redis;

  public TokenValidationService(IConfiguration config, IConnectionMultiplexer redis)
  {
    _config = config;
    _redis = redis;
  }

  public async Task<(bool isValid, long userId)> ValidateTokenAsync(string token)
  {
    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

      var validationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = _config["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = _config["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
      };

      var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
      var userId = long.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

      // Redis에서 토큰 유효성 검증 (로그아웃된 토큰인지 확인)
      var storedToken = await _redis.GetDatabase().StringGetAsync($"token:{userId}");
      if (!storedToken.HasValue || storedToken != token)
        return (false, 0);

      return (true, userId);
    }
    catch
    {
      return (false, 0);
    }
  }
}