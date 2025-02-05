using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

public static class NotificationHelper
{
  public static GamePacketMessage CreateJoinRoomNotification(UserData joinUser, List<int> targetUserIds)
  {
    var notification = new S2CJoinRoomNotification
    {
      JoinUser = joinUser
    };

    var gamePacket = new GamePacket();
    gamePacket.JoinRoomNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.JoinRoomNotification,
      gamePacket,
      targetUserIds
    );
  }

  public static GamePacketMessage CreateLeaveRoomNotification(int userId, int ownerId, List<int> targetUserIds)
  {
    var notification = new S2CLeaveRoomNotification
    {
      UserId = userId,
      OwnerId = ownerId
    };

    var gamePacket = new GamePacket();
    gamePacket.LeaveRoomNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.LeaveRoomNotification,
      gamePacket,
      targetUserIds
    );
  }

  public static GamePacketMessage CreateRoomReadyNotification(int userId, bool isReady, List<int> targetUserIds)
  {
    var notification = new S2CRoomReadyNotification
    {
      UserReady = new RoomUserReadyData
      {
        UserId = userId,
        IsReady = isReady
      }
    };

    var gamePacket = new GamePacket();
    gamePacket.RoomReadyNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.RoomReadyNotification,
      gamePacket,
      targetUserIds
    );
  }

  public static GamePacketMessage CreateChatMessageNotification(string nickname, string message, ChatMessageType messageType, List<int> targetUserIds)
  {
    var notification = new S2CChatMessageNotification
    {
      Nickname = nickname,
      Message = message,
      Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
      MessageType = messageType
    };

    var gamePacket = new GamePacket();
    gamePacket.ChatMessageNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.ChatMessageNotification, gamePacket, targetUserIds);
  }

  public static GamePacketMessage CreateGamePrepareNotification(RoomData room, List<int> targetUserIds)
  {
    var gamePacket = new GamePacket();
    gamePacket.GamePrepareNotification = new S2CGamePrepareNotification
    {
      Room = room
    };

    return GamePacketMessage.CreateBroadcast(
      PacketId.GamePrepareNotification,
      gamePacket,
      targetUserIds
    );
  }

  public static GamePacketMessage CreateGameStartNotification(ServerInfoData serverInfo, List<int> targetUserIds)
  {
    var notification = new S2CGameStartNotification
    {
      ServerInfo = serverInfo
    };

    var gamePacket = new GamePacket();
    gamePacket.GameStartNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.GameStartNotification, gamePacket, targetUserIds);
  }
}