using System.Collections.Immutable;
using System.Reflection;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketManager
{
  private static readonly Dictionary<PacketId, MessageParser> _parsers = new();
  private static uint _currentSequence = 0;

  static PacketManager()
  {
    // 사용 가능한 모든 PacketId 값 출력
    Console.WriteLine("사용 가능한 PacketId 목록:");
    foreach (PacketId id in Enum.GetValues(typeof(PacketId)))
    {
      Console.WriteLine($"  - {id} ({(int)id})");
    }

    // 리플렉션으로 자동 등록
    var assembly = Assembly.GetExecutingAssembly();
    var messageTypes = assembly.GetTypes()
      .Where(t => t.Namespace == "HolyShitServer.Src.Network.Packets" 
                  && typeof(IMessage).IsAssignableFrom(t)
                  && !t.IsAbstract);

    foreach (var type in messageTypes)
    {
      var parserProperty = type.GetProperty("Parser",
        BindingFlags.Public | BindingFlags.Static);

      if (parserProperty != null)
      {
        var parser = parserProperty.GetValue(null) as MessageParser;
        if (parser != null && Enum.TryParse<PacketId>(type.Name, out var packetId))
        {
          _parsers[packetId] = parser;
          Console.WriteLine($"Registered parser for: {type.Name}");
        }
      }
    }
  }

  public static IMessage? ParseMessage(PacketId packetId, ReadOnlySpan<byte> payload)
  {
    return _parsers.TryGetValue(packetId, out var parser)
      ? parser.ParseFrom(payload)
      : null;
  }

  public static async Task ProcessMessageAsync(PacketId id, uint sequence, IMessage message)
  {
    try 
    {
      Console.WriteLine($"메시지 처리 시작: ID={id}, Sequence={sequence}");
      
      // 등록된 핸들러 목록 출력
      var handlers = HandlerManager.GetRegisteredHandlers();
      Console.WriteLine($"등록된 핸들러 목록: {string.Join(", ", handlers)}");

      // 핸들러 존재 여부 확인
      var hasHandler = HandlerManager.HasHandler(id);
      Console.WriteLine($"핸들러 존재 여부: {hasHandler} for {id}");

      await HandlerManager.HandleMessageAsync(id, sequence, message);
      Console.WriteLine($"메시지 처리 완료: ID={id}, Sequence={sequence}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"메시지 처리 실패: ID={id}, Sequence={sequence}, Error={ex.Message}");
    }
  }

  // 새로운 시퀀스 번호 생성
  public static uint GetNextSequence()
  {
    return Interlocked.Increment(ref _currentSequence);
  }
}