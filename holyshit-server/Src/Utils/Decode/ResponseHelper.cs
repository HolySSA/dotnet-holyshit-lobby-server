using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Utils.Decode;

public static class ResponseHelper
{
  public static GamePacketMessage CreateRegisterResponse(
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
    
    return new GamePacketMessage(PacketId.RegisterResponse, sequence, response);
  }

  public static GamePacketMessage CreateLoginResponse(
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

    return new GamePacketMessage(PacketId.LoginResponse, sequence, response);
  }

  public static GamePacketMessage CreateGetRoomListResponse(
    uint sequence,
    List<RoomData> rooms)
  {
    var response = new S2CGetRoomListResponse();
    response.Rooms.AddRange(rooms);

    return new GamePacketMessage(PacketId.GetRoomListResponse, sequence, response);
  }

  public static GamePacketMessage CreateCreateRoomResponse(
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

    return new GamePacketMessage(PacketId.CreateRoomResponse, sequence, response);
  }

  /*
    // 에러 응답
    public static async Task SendErrorResponse(
        ClientSession client,
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