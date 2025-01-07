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

  public static GamePacketMessage CreateLeaveRoomNotification(int userId, List<int> targetUserIds)
  {
    var notification = new S2CLeaveRoomNotification
    {
      UserId = userId
    };

    var gamePacket = new GamePacket();
    gamePacket.LeaveRoomNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.LeaveRoomNotification,
      gamePacket,
      targetUserIds
    );
  }

  public static GamePacketMessage CreateGameReadyNotification(int userId, bool isReady, List<int> targetUserIds)
  {
    var notification = new S2CGameReadyNotification
    {
      UserId = userId,
      IsReady = isReady
    };

    var gamePacket = new GamePacket();
    gamePacket.GameReadyNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.GameReadyNotification,
      gamePacket,
      targetUserIds
    );
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

  public static GamePacketMessage CreateGameStartNotification(GameStateData gameState, List<UserData> users, List<CharacterPositionData> characterPositions, List<int> targetUserIds)
  {
    var notification = new S2CGameStartNotification
    {
      GameState = gameState
    };

    notification.Users.AddRange(users);
    notification.CharacterPositions.AddRange(characterPositions);

    var gamePacket = new GamePacket();
    gamePacket.GameStartNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.GameStartNotification, gamePacket, targetUserIds);
  }

  public static GamePacketMessage CreatePositionUpdateNotification(List<CharacterPositionData> characterPositions, List<int> targetUserIds)
  {
    var notification = new S2CPositionUpdateNotification();
    notification.CharacterPositions.AddRange(characterPositions);

    var gamePacket = new GamePacket();
    gamePacket.PositionUpdateNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.PositionUpdateNotification, gamePacket, targetUserIds);
  }
}