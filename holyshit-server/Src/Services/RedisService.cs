using System.IdentityModel.Tokens.Jwt;
using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.DB.Entities;
using HolyShitServer.Src.Network.Packets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HolyShitServer.Src.Services;

public class RedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;
    private const string SESSION_KEY_FORMAT = "session:{0}";
    private const string USER_KEY_FORMAT = "user:{0}";
    private const string USER_CHARACTERS_KEY_FORMAT = "user:{0}:characters";

    public RedisService(IConnectionMultiplexer redis, IConfiguration config)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<(bool isValid, int userId)> ValidateTokenAsync(string token)
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

            var userId = int.Parse(jwtToken.Claims.First(c => c.Type == "nameid").Value);
            return (true, userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenValidation] 토큰 검증 실패: {ex.Message}");
            return (false, 0);
        }
    }

    /// <summary>
    /// 유저 정보를 Redis에 캐싱
    /// </summary>
    public async Task CacheUserCharacterTypeAsync(User user)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_KEY_FORMAT, user.Id);
            
            var hashFields = new HashEntry[]
            {
                new HashEntry("Id", user.Id),
                new HashEntry("Nickname", user.Nickname),
                new HashEntry("LastSelectedCharacter", (int)user.LastSelectedCharacter),
                // 필요한 다른 유저 정보도 여기에 추가
            };

            await db.HashSetAsync(key, hashFields);
            // 캐시 만료 시간 설정 (24시간)
            await db.KeyExpireAsync(key, TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 유저 데이터 캐싱 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 유저의 보유 캐릭터 정보를 Redis에 캐싱
    /// </summary>
    public async Task CacheUserCharactersAsync(int userId, List<CharacterType> characters)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_CHARACTERS_KEY_FORMAT, userId);
            
            // 기존 캐시 삭제
            await db.KeyDeleteAsync(key);
            
            // 새로운 캐릭터 목록 캐싱
            if (characters.Any())
            {
                var values = characters.Select(c => (RedisValue)((int)c)).ToArray();
                await db.SetAddAsync(key, values);
                // 캐시 만료 시간 설정 (24시간)
                await db.KeyExpireAsync(key, TimeSpan.FromHours(24));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 유저 캐릭터 캐싱 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 유저 현재 캐릭터 조회 (없으면 DB에서 조회 후 캐싱)
    /// </summary>
    public async Task<UserCharacterTypeData?> GetUserCharacterTypeAsync(int userId, ApplicationDbContext dbContext)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_KEY_FORMAT, userId);

            // Redis에서 먼저 조회
            var hashFields = await db.HashGetAllAsync(key);
            if (hashFields.Length > 0)
            {
                // Redis에 데이터가 있으면 변환해서 반환
                var userDict = hashFields.ToDictionary(
                    h => h.Name.ToString(),
                    h => h.Value.ToString()
                );

                return new UserCharacterTypeData
                {
                    Id = userId,
                    Nickname = userDict["Nickname"],
                    LastSelectedCharacter = (CharacterType)int.Parse(userDict["LastSelectedCharacter"])
                };
            }

            // Redis 존재 X 시 DB에서 조회
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                // DB에서 찾았으면 Redis에 캐싱
                await CacheUserCharacterTypeAsync(user);
                return new UserCharacterTypeData
                {
                    Id = user.Id,
                    Nickname = user.Nickname,
                    LastSelectedCharacter = user.LastSelectedCharacter
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 유저 정보 조회 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 유저 보유 캐릭터 목록 조회 (없으면 DB에서 조회 후 캐싱)
    /// </summary>
    public async Task<HashSet<CharacterType>> GetUserCharactersAsync(int userId, ApplicationDbContext dbContext)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_CHARACTERS_KEY_FORMAT, userId);

            // Redis에서 먼저 조회
            var characters = await db.SetMembersAsync(key);
            if (characters.Any())
            {
                // Redis에 데이터가 있으면 변환해서 반환
                return characters.Select(c => (CharacterType)int.Parse(c.ToString())).ToHashSet();
            }

            // Redis에 없으면 DB에서 조회
            var userCharacters = await dbContext.UserCharacters
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CharacterType)
                .ToListAsync();

            // DB에서 찾은 데이터를 Redis에 캐싱
            await CacheUserCharactersAsync(userId, userCharacters);
            return userCharacters.ToHashSet();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 유저 캐릭터 목록 조회 실패: {ex.Message}");
            return new HashSet<CharacterType>();
        }
    }

    /// <summary>
    /// 선택한 캐릭터 정보 Redis에 업데이트
    /// </summary>
    public async Task UpdateUserSelectedCharacterAsync(int userId, CharacterType characterType)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_KEY_FORMAT, userId);
            
            // LastSelectedCharacter 필드만 업데이트
            await db.HashSetAsync(key, new HashEntry[]
            {
                new HashEntry("LastSelectedCharacter", (int)characterType)
            });
            
            // 캐시 만료 시간 갱신
            await db.KeyExpireAsync(key, TimeSpan.FromHours(24));
            Console.WriteLine($"[Redis] 선택 캐릭터 업데이트 성공. UserId: {userId}, Character: {characterType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 선택 캐릭터 업데이트 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Redis의 선택 캐릭터 정보를 DB에 동기화
    /// </summary>
    public async Task SyncSelectedCharacterToDbAsync(int userId, ApplicationDbContext dbContext)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = string.Format(USER_KEY_FORMAT, userId);
            
            var lastSelectedCharacter = await db.HashGetAsync(key, "LastSelectedCharacter");
            if (!lastSelectedCharacter.HasValue)
            {
                return;
            }

            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastSelectedCharacter = (CharacterType)(int)lastSelectedCharacter;
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"[Redis] DB 동기화 성공. UserId: {userId}, Character: {user.LastSelectedCharacter}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] DB 동기화 실패: {ex.Message}");
        }
    }
}

public class UserCharacterTypeData
{
    public int Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public CharacterType LastSelectedCharacter { get; set; }
}