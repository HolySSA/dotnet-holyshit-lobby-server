using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Utils.Decode;

public static class ResponseHelper
{
  public static GamePacketMessage CreateLoginResponse(
    uint sequence,
    bool success,
    List<CharacterInfoData> characters,
    CharacterType lastSelectedCharacter,
    GlobalFailCode failCode = GlobalFailCode.NoneFailcode)
  {
    var response = new S2CLoginResponse
    {
      Success = success,
      LastSelectedCharacter = lastSelectedCharacter,
      FailCode = failCode,
    };

    if (characters != null)
    {
      response.Characters.AddRange(characters);
    }

    Console.WriteLine($"[Response] Login Response 생성: Success={success}, FailCode={failCode}");

    var gamePacket = new GamePacket();
    gamePacket.LoginResponse = response;

    return new GamePacketMessage(PacketId.LoginResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateSelectCharacterResponse(
    uint sequence,
    bool success,
    CharacterType characterType,
    GlobalFailCode failCode)
  {
    var response = new S2CSelectCharacterResponse
    {
      Success = success,
      CharacterType = characterType,
      FailCode = failCode
    };

    var gamePacket = new GamePacket();
    gamePacket.SelectCharacterResponse = response;

    return new GamePacketMessage(PacketId.SelectCharacterResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateGetRoomListResponse(
    uint sequence,
    List<RoomData>? rooms)
  {
    var response = new S2CGetRoomListResponse();
    response.Rooms.AddRange(rooms);

    var gamePacket = new GamePacket();
    gamePacket.GetRoomListResponse = response;

    return new GamePacketMessage(PacketId.GetRoomListResponse, sequence, gamePacket);
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

    var gamePacket = new GamePacket();
    gamePacket.CreateRoomResponse = response;

    return new GamePacketMessage(PacketId.CreateRoomResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateJoinRoomResponse(
    uint sequence,
    bool success,
    RoomData? room,
    GlobalFailCode failCode)
  {
    var response = new S2CJoinRoomResponse
    {
      Success = success,
      FailCode = failCode,
      Room = room,
    };

    Console.WriteLine($"[Response] JoinRoom Response 생성: Success={success}, FailCode={failCode}");
    if (room != null)
    {
      Console.WriteLine($"[Response] Room 정보: Id={room.Id}, Name='{room.Name}', Owner={room.OwnerId}, Users={room.Users.Count}");
    }

    var gamePacket = new GamePacket();
    gamePacket.JoinRoomResponse = response;

    return new GamePacketMessage(PacketId.JoinRoomResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateJoinRandomRoomResponse(
    uint sequence,
    bool success,
    RoomData? room,
    GlobalFailCode failCode)
  {
    var response = new S2CJoinRandomRoomResponse
    {
      Success = success,
      FailCode = failCode,
      Room = room,
    };

    var gamePacket = new GamePacket();
    gamePacket.JoinRandomRoomResponse = response;

    return new GamePacketMessage(PacketId.JoinRandomRoomResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateLeaveRoomResponse(uint sequence, bool success, GlobalFailCode failCode)
  {
    var response = new S2CLeaveRoomResponse
    {
      Success = success,
      FailCode = failCode,
    };

    var gamePacket = new GamePacket();
    gamePacket.LeaveRoomResponse = response;

    return new GamePacketMessage(PacketId.LeaveRoomResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateGameReadyResponse(uint sequence, bool success, GlobalFailCode failCode)
  {
    var gamePacket = new GamePacket();
    gamePacket.GameReadyResponse = new S2CGameReadyResponse
    {
      Success = success,
      FailCode = failCode
    };

    return new GamePacketMessage(PacketId.GameReadyResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateGamePrepareResponse(uint sequence, bool success, GlobalFailCode failCode)
  {
    var gamePacket = new GamePacket();
    gamePacket.GamePrepareResponse = new S2CGamePrepareResponse
    {
      Success = success,
      FailCode = failCode
    };

    return new GamePacketMessage(PacketId.GamePrepareResponse, sequence, gamePacket);
  }

  public static GamePacketMessage CreateGameStartResponse(uint sequence, bool success, GlobalFailCode failCode)
  {
    var gamePacket = new GamePacket();
    gamePacket.GameStartResponse = new S2CGameStartResponse
    {
      Success = success,
      FailCode = failCode
    };

    return new GamePacketMessage(PacketId.GameStartResponse, sequence, gamePacket);
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