using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;

namespace HolyShitServer.Src.Network.Socket;

public class GamePacketMessage
{
  public PacketId PacketId { get; }
  public uint Sequence { get; }
  public IMessage Message { get; }
  public List<int> TargetUserIds { get; }

  public GamePacketMessage(PacketId packetId, uint sequence, IMessage message, List<int>? targetUserIds = null)
  {
    PacketId = packetId;
    Sequence = sequence;
    Message = message;
    TargetUserIds = targetUserIds ?? new List<int>();
  }

  // 브로드캐스트 메시지 생성
  public static GamePacketMessage CreateBroadcast(PacketId packetId, IMessage message, List<int> targetUserIds)
  {
    return new GamePacketMessage(packetId, PacketManager.GetNextSequence(), message, targetUserIds);
  }

  // 단일 대상 메시지 생성
  public static GamePacketMessage CreateSingle(PacketId packetId, IMessage message)
  {
    return new GamePacketMessage(packetId, PacketManager.GetNextSequence(), message);
  }

  // 빈 메시지 생성을 위한 팩토리 메서드
  public static GamePacketMessage CreateEmpty(PacketId packetId, uint sequence)
  {
    var emptyPacket = new GamePacket();  // 빈 GamePacket 생성
    return new GamePacketMessage(packetId, sequence, emptyPacket);
  }
}