using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Utils.Decode;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  private static TokenValidationService? _tokenValidationService;
  private static IConnectionMultiplexer? _redis;
  private static ApplicationDbContext? _dbContext;
  private static GameDataManager? _gameDataManager;

  /// <summary>
  /// 서비스 초기화
  /// </summary>
  public static void Initialize(
    TokenValidationService tokenValidationService,
    IConnectionMultiplexer redis,
    ApplicationDbContext dbContext,
    GameDataManager gameDataManager)
  {
    _tokenValidationService = tokenValidationService;
    _redis = redis;
    _dbContext = dbContext;
    _gameDataManager = gameDataManager;
  }

  /// <summary>
  /// 로그인 요청 처리
  /// </summary>
  public static async Task<GamePacketMessage> HandleLoginRequest(ClientSession client, uint sequence, C2SLoginRequest request)
  {
    try
    {
      if (_tokenValidationService == null || _redis == null || _dbContext == null || _gameDataManager == null)
        throw new InvalidOperationException("Services not initialized");

      // 토큰 검증
      var (isValid, userId) = await _tokenValidationService.ValidateTokenAsync(request.Token);
      if (!isValid)
        return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.AuthenticationFailed);

      // 유저 조회
      var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
      if (user == null)
        return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.AuthenticationFailed);

      // 전체 캐릭터 조회
      var allCharacters = _gameDataManager.GetAllCharacters();
      // 유저 보유 캐릭터 조회
      var userCharacters = await _dbContext.UserCharacters.Where(uc => uc.UserId == userId).ToListAsync();

      // 캐릭터 리스트
      var characterInfoList = allCharacters.Select(c => new CharacterInfoData
      {
        CharacterType = Enum.Parse<CharacterType>(c.Type),
        Name = c.Name,
        Description = c.Description,
        Hp = c.BaseHp,
        Owned = userCharacters.Any(uc => uc.CharacterType == Enum.Parse<CharacterType>(c.Type)),
        PlayCount = userCharacters.FirstOrDefault(uc => uc.CharacterType == Enum.Parse<CharacterType>(c.Type))?.PlayCount ?? 0,
        WinCount = userCharacters.FirstOrDefault(uc => uc.CharacterType == Enum.Parse<CharacterType>(c.Type))?.WinCount ?? 0
      }).ToList();

      return ResponseHelper.CreateLoginResponse(sequence, true, characterInfoList, user.LastSelectedCharacter, GlobalFailCode.NoneFailcode);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"로그인 처리 중 오류 발생: {ex.Message}");
      return ResponseHelper.CreateLoginResponse(sequence, false, new List<CharacterInfoData>(), CharacterType.NoneCharacter, GlobalFailCode.UnknownError);
    }
  }
}