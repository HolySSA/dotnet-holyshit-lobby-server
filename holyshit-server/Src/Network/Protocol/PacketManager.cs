using System.Collections.Immutable;
using System.Reflection;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketManager
{
  private static readonly Dictionary<GamePacket.PayloadOneofCase, PropertyInfo> _propertyCache = new();
  private static uint _currentSequence = 0;
  private static bool _isInitialized = false; // 초기화 여부
  private static readonly object _initLock = new object();

  public static void Initialize()
  {
    if (_isInitialized) return;

    lock (_initLock)
    {
      if (_isInitialized) return;
      
      // 프로퍼티 캐시 초기화
      foreach (var payloadCase in Enum.GetValues<GamePacket.PayloadOneofCase>())
      {
        if (payloadCase == GamePacket.PayloadOneofCase.None) continue;
        
        var property = typeof(GamePacket).GetProperty(payloadCase.ToString());
        if (property != null)
        {
          _propertyCache[payloadCase] = property;
        }
      }

      _isInitialized = true;
      Console.WriteLine("PacketManager 초기화 완료");
    }
  }
  
  public static IMessage? ParseMessage(ReadOnlySpan<byte> payload)
  {
    if (!_isInitialized)
    {
      throw new InvalidOperationException("PacketManager가 초기화되지 않았습니다.");
    }

    try
    {
      var gamePacket = GamePacket.Parser.ParseFrom(payload.ToArray());
      
      if (_propertyCache.TryGetValue(gamePacket.PayloadCase, out var property))
      {
        return property.GetValue(gamePacket) as IMessage;
      }
      
      return null;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"메시지 파싱 중 오류: {ex.Message}");
      return null;
    }
}

  public static async Task ProcessMessageAsync(ClientSession client, PacketId id, uint sequence, IMessage message)
  {
    if (!_isInitialized)
    {
      throw new InvalidOperationException("PacketManager가 초기화되지 않았습니다.");
    }
    
    try
    {
      await HandlerManager.HandleMessageAsync(client, id, sequence, message);
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