using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

public class TokenValidationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;
    private const string SESSION_KEY_FORMAT = "session:{0}";

    public TokenValidationService(IConnectionMultiplexer redis, IConfiguration config)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<(bool isValid, long userId)> ValidateTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("[TokenValidation] 토큰에 이메일 정보가 없습니다.");
                return (false, 0);
            }

            var db = _redis.GetDatabase();
            var sessionKey = string.Format(SESSION_KEY_FORMAT, email);

            // 토큰 필드만 직접 조회
            var storedToken = await db.HashGetAsync(sessionKey, "Token");
            if (storedToken.IsNull || storedToken.ToString() != token)
            {
                return (false, 0);
            }

            var userId = long.Parse(jwtToken.Claims.First(c => c.Type == "nameid").Value);
            return (true, userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenValidation] 토큰 검증 실패: {ex.Message}");
            return (false, 0);
        }
    }
}

public class SessionData
{
    public string Token { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
}