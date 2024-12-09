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

    Console.WriteLine($"[Response] Register Response 생성: Success={success}, Message='{message}', FailCode={failCode}");
    Console.WriteLine($"[Response] Register Response 객체: {response}");

    await client.SendResponseAsync(PacketId.RegisterResponse, sequence, response);
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

    Console.WriteLine($"[Response] Login Response 생성: Success={success}, Message='{message}', Token='{token}', FailCode={failCode}");
    Console.WriteLine($"[Response] Login Response UserData: {myInfo}");
    Console.WriteLine($"[Response] Login Response 객체: {response}");

    var gamePacket = new GamePacket();
    gamePacket.LoginResponse = response;

    Console.WriteLine($"[Response] GamePacket 생성: {gamePacket}");
    Console.WriteLine($"[Response] Serialized Size: {gamePacket.CalculateSize()}");

    await client.SendResponseAsync(PacketId.LoginResponse, sequence, response);

    Console.WriteLine("[Response] Login Response 전송 완료");
  }

  public static async Task SendGetRoomListResponse(
    TcpClientHandler client,
    uint sequence,
    List<RoomData> rooms)
  {
    var response = new S2CGetRoomListResponse();
    response.Rooms.AddRange(rooms);

    var gamePacket = new GamePacket();
    gamePacket.GetRoomListResponse = response;

    await client.SendResponseAsync(PacketId.GetRoomListResponse, sequence, response);

    Console.WriteLine("[Response] GetRoomList Response 전송 완료");
  }

  public static async Task SendCreateRoomResponse(
    TcpClientHandler client,
    uint sequence,
    bool success,
    RoomData? room,
    GlobalFailCode failCode)
  {
    var response = new S2CCreateRoomResponse
    {
      Success = success,
      Room = room,
      FailCode = failCode
    };

    Console.WriteLine($"[Response] CreateRoom Response 생성: Success={success}, FailCode={failCode}");
    if (room != null)
    {
      Console.WriteLine($"[Response] Room 정보: Id={room.Id}, Name='{room.Name}', Owner={room.OwnerId}, Users={room.Users.Count}");
    }

    await client.SendResponseAsync(PacketId.CreateRoomResponse, sequence, response);
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