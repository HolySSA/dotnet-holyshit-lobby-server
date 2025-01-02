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
  private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

  public TokenValidationService(IConfiguration config, IConnectionMultiplexer redis)
  {
    _config = config;
    _redis = redis;

    // 설정값 검증 및 초기화
    _jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured");
    _jwtIssuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured");
    _jwtAudience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured");
  }

  public async Task<(bool isValid, long userId)> ValidateTokenAsync(string token)
  {
    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_jwtKey);

      var validationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = _jwtIssuer,
        ValidateAudience = true,
        ValidAudience = _jwtAudience,
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