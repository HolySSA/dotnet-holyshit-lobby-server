using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;

namespace HolyShitServer.Src.Network.Socket;

public class GamePacketMessage
{
  public PacketId PacketId { get; }
  public uint Sequence { get; }
  public IMessage Message { get; }
  public List<string> TargetSessionIds { get; }

  public GamePacketMessage(PacketId packetId, uint sequence, IMessage message, List<string>? targetSessionIds = null)
  {
    if (message == null)
    {
        throw new ArgumentNullException(nameof(message), "메시지는 null일 수 없습니다.");
    }

    if (!Enum.IsDefined(typeof(PacketId), packetId))
    {
        throw new ArgumentException($"알 수 없는 패킷 ID입니다: {packetId}");
    }

    PacketId = packetId;
    Sequence = sequence;
    Message = message;
    TargetSessionIds = targetSessionIds ?? new List<string>();
  }

  // 브로드캐스트 메시지 생성을 위한 팩토리 메서드
  public static GamePacketMessage CreateBroadcast(PacketId packetId, IMessage message, List<string> targetSessionIds)
  {
    return new GamePacketMessage(packetId, PacketManager.GetNextSequence(), message, targetSessionIds);
  }

  // 단일 대상 메시지 생성을 위한 팩토리 메서드
  public static GamePacketMessage CreateSingle(PacketId packetId, IMessage message, string targetSessionId)
  {
    return new GamePacketMessage(packetId, PacketManager.GetNextSequence(), message, new List<string> { targetSessionId });
  }

  // 빈 메시지 생성을 위한 팩토리 메서드
  public static GamePacketMessage CreateEmpty(PacketId packetId, uint sequence)
  {
    var emptyPacket = new GamePacket();  // 빈 GamePacket 생성
    return new GamePacketMessage(packetId, sequence, emptyPacket);
  }
}