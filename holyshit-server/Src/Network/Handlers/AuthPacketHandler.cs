using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Utils.Decode;
using StackExchange.Redis;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  private static TokenValidationService? _tokenValidationService;
  private static IConnectionMultiplexer? _redis;

  // 서비스 초기화
  public static void Initialize(TokenValidationService tokenValidationService, IConnectionMultiplexer redis)
  {
    _tokenValidationService = tokenValidationService;
    _redis = redis;
  }

  /*
    public static async Task<GamePacketMessage> HandleLoginRequest(ClientSession client, uint sequence, C2SLoginRequest request)
    {
      try
      {
        if (_tokenValidationService == null || _redis == null)
          throw new InvalidOperationException("Services not initialized");

        // 토큰 검증
        var (isValid, userId) = await _tokenValidationService.ValidateTokenAsync(request.Token);
        if (!isValid)
          return ResponseHelper.CreateLoginResponse(sequence, false, null, GlobalFailCode.NoneFailcode);
      }
      catch (Exception ex)
      {
        return ResponseHelper.CreateLoginResponse(sequence, false, null, GlobalFailCode.NoneFailcode);
      }
    }
    */
}