using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

public static class NotificationHelper
{
  public static GamePacketMessage CreateJoinRoomNotification(
    UserData joinUser,
    List<string> targetSessionIds
  )
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

  public static GamePacketMessage CreateLeaveRoomNotification(
    long userId,
    List<string> targetSessionIds
  )
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
}