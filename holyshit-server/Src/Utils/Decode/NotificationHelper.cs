using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

public static class NotificationHelper
{
  public static GamePacketMessage CreateJoinRoomNotification(UserData joinUser, List<string> targetSessionIds)
  {
    var notification = new S2CJoinRoomNotification
    {
      JoinUser = joinUser
    };

    Console.WriteLine($"[Notification] JoinRoom Notification 생성: JoinUserId={joinUser.Id}, Targets={targetSessionIds.Count}명");

    var gamePacket = new GamePacket();
    gamePacket.JoinRoomNotification = notification;

    return GamePacketMessage.CreateBroadcast(
      PacketId.JoinRoomNotification,
      gamePacket,
      targetSessionIds
    );
  }

  public static GamePacketMessage CreateLeaveRoomNotification(long userId, List<string> targetSessionIds)
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
      targetSessionIds
    );
  }

  public static GamePacketMessage CreateGameReadyNotification(long userId, bool isReady, List<string> targetSessionIds)
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
      targetSessionIds
    );
  }

  public static GamePacketMessage CreateGamePrepareNotification(RoomData room, List<string> targetSessionIds)
  {
    var gamePacket = new GamePacket();
    gamePacket.GamePrepareNotification = new S2CGamePrepareNotification
    {
      Room = room
    };

    return GamePacketMessage.CreateBroadcast(
      PacketId.GamePrepareNotification,
      gamePacket,
      targetSessionIds
    );
  }

  public static GamePacketMessage CreateGameStartNotification(GameStateData gameState, List<UserData> users, List<CharacterPositionData> characterPositions, List<string> targetSessionIds)
  {
    var notification = new S2CGameStartNotification
    {
      GameState = gameState
    };
    
    notification.Users.AddRange(users);
    notification.CharacterPositions.AddRange(characterPositions);

    var gamePacket = new GamePacket();
    gamePacket.GameStartNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.GameStartNotification, gamePacket, targetSessionIds);
  }

  public static GamePacketMessage CreatePositionUpdateNotification(List<CharacterPositionData> characterPositions, List<string> targetSessionIds)
  {
    var notification = new S2CPositionUpdateNotification();
    notification.CharacterPositions.AddRange(characterPositions);

    var gamePacket = new GamePacket();
    gamePacket.PositionUpdateNotification = notification;

    return GamePacketMessage.CreateBroadcast(PacketId.PositionUpdateNotification, gamePacket, targetSessionIds);
  }
}