using System.Collections.Immutable;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketManager
{
  private static readonly ImmutableDictionary<PacketId, MessageParser> _parser;
  private static readonly ImmutableDictionary<Type, PacketId> _packetTypes;

  private static uint _currentSequence = 0;

  static PacketManager()
  {
    var parserBuilder = ImmutableDictionary.CreateBuilder<PacketId, MessageParser>();
    var typeBuilder = ImmutableDictionary.CreateBuilder<Type, PacketId>();

    InitializeMessageTypes(parserBuilder, typeBuilder);
    
    _parsers = parserBuilder.ToImmutable();
    _packetTypes = typeBuilder.ToImmutable();
  }

  // 모든 메시지 타입 초기화
  private static void InitializeMessageTypes(
    ImmutableDictionary<PacketId, MessageParser>.Builder parserBuilder,
    ImmutableDictionary<Type, PacketId>.Builder typeBuilder)
  {
    // 리플렉션으로 자동 등록
    var assembly = Assembly.GetExecutingAssembly();
    var messageTypes = assembly.GetTypes()
      .Where(t => t.Namespace == "HolyShitServer.Src.Network.Packets" 
                  && typeof(IMessage).IsAssignableFrom(t) 
                  && !t.IsAbstract);

    foreach(var type in messageTypes)
    {
      if(Enum.TryParse<PacketId>(type.Name, out var packetId))
      {
        // Parser 속성 가져오기
        var parserProperty = type.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
        if(parserProperty != null)
        {
          var parser = parserProperty.GetValue(null) as MessageParser;
          if(parser != null)
          {
            parserBuilder.Add(packetId, parser);
            typeBuilder.Add(type, packetId);
           Console.WriteLine($"Initialized packet: {type.Name}");
          }
        }
      }

      Console.WriteLine($"Total message types: {parserBuilder.Count}");
    }
  }

  // 새로운 시퀀스 번호 생성
  public static uint GetNextSequence()
  {
    return Interlocked.Increment(ref _currentSequence);
  }

  // 메시지 처리
  public static async Task ProcessMessageAsync(PacketId id, uint sequence, IMessage message)
  {
    await HandlerManager.HandleMessageAsync(id, sequence, message);
  }
}