using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HolyShitServer.Src.Services;

public class JwtTokenService
{
  private readonly IConfiguration _configuration;

  public JwtTokenService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public string GenerateGameServerToken(int userId, int roomId)
  {
    var secretKey = _configuration["JwtSettings:SecretKey"];
    if (string.IsNullOrEmpty(secretKey))
    {
      throw new InvalidOperationException("JWT SecretKey가 설정되지 않았습니다.");
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
      new Claim("RoomId", roomId.ToString())
    };

    var token = new JwtSecurityToken(
      issuer: _configuration["JwtSettings:Issuer"] ?? "LobbyServer",
      audience: _configuration["JwtSettings:Audience"] ?? "GameServer",
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(30), // 토큰 만료 시간
      signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}