using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Utils.Decode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  private static readonly Dictionary<string, CharacterType> CharacterTypeMap = new()
  {
    { "RED", CharacterType.Red },
    { "SHARK", CharacterType.Shark },
    { "MALANG", CharacterType.Malang },
    { "FROGGY", CharacterType.Froggy },
    { "PINK", CharacterType.Pink },
    { "SWIM_GLASSES", CharacterType.SwimGlasses },
    { "MASK", CharacterType.Mask },
    { "DINOSAUR", CharacterType.Dinosaur },
    { "PINK_SLIME", CharacterType.PinkSlime }
  };

  /// <summary>
  /// 로그인 요청 처리
  /// </summary>
  public static async Task<GamePacketMessage> HandleLoginRequest(ClientSession client, uint sequence, C2SLoginRequest request)
  {
    try
    {
      // 토큰이 없는 요청은 무시
      if (string.IsNullOrEmpty(request.Token))
      {
        return ResponseHelper.CreateLoginResponse(
          sequence,
          false,
          new List<CharacterInfoData>(),
          CharacterType.NoneCharacter,
          GlobalFailCode.AuthenticationFailed
        );
      }

      using var scope = client.ServiceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var gameDataManager = scope.ServiceProvider.GetRequiredService<GameDataManager>();

      // 토큰 검증
      var (isValid, userId) = await redisService.ValidateTokenAsync(request.Token);
      if (!isValid)
      {
        Console.WriteLine("[Auth] 토큰 검증 실패");
        return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.AuthenticationFailed);
      }

      // 유저 조회
      var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
      if (user == null)
      {
        Console.WriteLine($"[Auth] 유저 정보 없음. UserId: {userId}");
        return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.AuthenticationFailed);
      }

      // 세션에 유저 ID 설정
      client.SetUserId(userId);

      // 전체 캐릭터 조회
      var allCharacters = gameDataManager.GetAllCharacters();

      // 유저 보유 캐릭터 조회
      var userCharacters = await dbContext.UserCharacters.Where(uc => uc.UserId == userId).ToListAsync();

      // Redis에 유저 현재 캐릭터 정보 캐싱
      await redisService.CacheUserCharacterTypeAsync(user);
      // Redis에 유저의 보유 캐릭터 정보 캐싱
      var userCharacterStats = userCharacters.Select(uc => new UserCharacterStatsData
      {
        CharacterType = uc.CharacterType,
        PlayCount = uc.PlayCount,
        WinCount = uc.WinCount
      }).ToList();
      await redisService.CacheUserCharactersAsync(userId, userCharacterStats);

      // 캐릭터 리스트
      var characterInfoList = allCharacters.Where(c => c != null).Select(c =>
      {
        if (!CharacterTypeMap.TryGetValue(c.Type, out var characterType))
        {
          Console.WriteLine($"[Auth] 캐릭터 타입 매핑 실패: {c.Type}");
          return null;
        }

        var userCharacter = userCharacters.FirstOrDefault(uc => uc.CharacterType == characterType);
        return new CharacterInfoData
        {
          CharacterType = characterType,
          Name = c.Name,
          Description = c.Description,
          Hp = c.BaseHp,
          Owned = userCharacter != null,
          PlayCount = userCharacter?.PlayCount ?? 0,
          WinCount = userCharacter?.WinCount ?? 0
        };
      }).Where(c => c != null).Select(c => c!).ToList();

      Console.WriteLine($"[Auth] 로그인 성공. UserId: {userId}");
      return ResponseHelper.CreateLoginResponse(sequence, true, characterInfoList, user.LastSelectedCharacter, GlobalFailCode.NoneFailcode);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Auth] 로그인 처리 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
      return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.UnknownError);
    }
  }

  public static async Task<GamePacketMessage> HandleSelectCharacterRequest(ClientSession client, uint sequence, C2SSelectCharacterRequest request)
  {
    try
    {
      using var scope = client.ServiceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 조회
      var user = await redisService.GetUserCharacterTypeAsync(client.UserId, dbContext);
      if (user == null)
      {
        return ResponseHelper.CreateSelectCharacterResponse(
          sequence,
          false,
          CharacterType.NoneCharacter,
          GlobalFailCode.AuthenticationFailed
        );
      }

      // 캐릭터 보유 여부 확인
      if (!await redisService.HasCharacterAsync(client.UserId, request.CharacterType, dbContext))
      {
        return ResponseHelper.CreateSelectCharacterResponse(
          sequence,
          false,
          CharacterType.NoneCharacter,
          GlobalFailCode.CharacterNotFound
        );
      }

      // 마지막 선택 캐릭터 업데이트
      await redisService.UpdateUserSelectedCharacterAsync(client.UserId, request.CharacterType);

      Console.WriteLine($"[Auth] 캐릭터 선택 성공. UserId: {client.UserId}, Character: {request.CharacterType}");
      return ResponseHelper.CreateSelectCharacterResponse(
        sequence,
        true,
        request.CharacterType,
        GlobalFailCode.NoneFailcode
      );
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Auth] 캐릭터 선택 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
      return ResponseHelper.CreateSelectCharacterResponse(
        sequence,
        false,
        CharacterType.NoneCharacter,
        GlobalFailCode.UnknownError
      );
    }
  }
}