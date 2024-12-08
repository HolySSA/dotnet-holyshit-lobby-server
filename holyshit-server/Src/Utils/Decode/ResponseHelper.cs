using Google.Protobuf;
using HolyShitServer.Src.Network;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Utils.Decode;

public static class ResponseHelper
{
    public static async Task SendResponse<T>(
      TcpClientHandler client,
      PacketId packetId,
      uint sequence,
      T message) where T : IMessage
    {
      await client.SendResponseAsync(packetId, sequence, message);
    }

    // 회원가입 응답
    public static async Task SendRegisterResponse(
      TcpClientHandler client,
      uint sequence,
      bool success,
      string message,
      GlobalFailCode failCode)
    {
      var response = new S2CRegisterResponse
      {
        Success = success,
        Message = message,
        FailCode = failCode
      };

      await SendResponse(client, PacketId.S2CregisterResponse, sequence, response);
    }

    // 로그인 응답
    public static async Task SendLoginResponse(
      TcpClientHandler client,
      uint sequence,
      bool success,
      string message,
      string? token = null,
      UserData? myInfo = null,
      GlobalFailCode failCode = GlobalFailCode.NoneFailcode)
    {
      var response = new S2CLoginResponse
      {
        Success = success,
        Message = message,
        Token = token ?? string.Empty,
        MyInfo = myInfo,
        FailCode = failCode,
      };

      await SendResponse(client, PacketId.S2CloginResponse, sequence, response);
    }

  /*
    // 에러 응답
    public static async Task SendErrorResponse(
        TcpClientHandler client,
        uint sequence,
        string message,
        GlobalFailCode failCode)
    {
        var response = new S2CErrorResponse
        {
            Message = message,
            FailCode = failCode
        };

        await SendResponse(client, PacketId.S2CerrorResponse, sequence, response);
    }
  */
}