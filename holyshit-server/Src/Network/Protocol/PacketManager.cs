using System.Collections.Immutable;
using System.Reflection;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketManager
{
  private static readonly Dictionary<PacketId, MessageParser> _parsers = new();
  private static readonly Dictionary<GamePacket.PayloadOneofCase, PropertyInfo> _propertyCache = new();

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
      // 타입 이름에서 PacketId enum 값 추출
      var typeName = type.Name;
      if (Enum.TryParse<PacketId>(typeName, true, out var packetId))
      {
        // Parser 프로퍼티 가져오기
        var parserProperty = type.GetProperty("Parser",
          BindingFlags.Public | BindingFlags.Static);

        if (parserProperty != null)
        {
          var parser = parserProperty.GetValue(null) as MessageParser;
          if (parser != null)
          {
            _parsers[packetId] = parser;
            Console.WriteLine($"파서 등록 성공: {typeName} -> {packetId} ({(int)packetId})");
          }
          else
          {
            Console.WriteLine($"파서 가져오기 실패: {typeName}");
          }
        }
        else
        {
          Console.WriteLine($"Parser 프로퍼티 없음: {typeName}");
        }
      }
      else
      {
        Console.WriteLine($"PacketId enum 매칭 실패: {typeName}");
      }
    }

    // 등록된 모든 파서 출력
    Console.WriteLine("\n등록된 파서 목록:");
    foreach (var parser in _parsers)
    {
      Console.WriteLine($"  - {parser.Key} ({(int)parser.Key}): {parser.Value.GetType().Name}");
    }
  }
  
  public static IMessage? ParseMessage(PacketId packetId, ReadOnlySpan<byte> payload)
{
    try
    {
      var gamePacket = GamePacket.Parser.ParseFrom(payload.ToArray());
      
      // 캐시된 프로퍼티 정보 찾기
      if (!_propertyCache.TryGetValue(gamePacket.PayloadCase, out var property))
      {
        property = typeof(GamePacket).GetProperty(gamePacket.PayloadCase.ToString());
        if (property == null)
        {
          Console.WriteLine($"알 수 없는 페이로드 타입: {gamePacket.PayloadCase}");
          return null;
        }

        _propertyCache[gamePacket.PayloadCase] = property;
      }
      
      return property.GetValue(gamePacket) as IMessage;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"메시지 파싱 중 오류: {ex.Message}");
      return null;
    }
}

  public static async Task ProcessMessageAsync(PacketId id, uint sequence, IMessage message)
  {
    try
    {
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