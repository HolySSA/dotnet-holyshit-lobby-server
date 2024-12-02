using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public static class HandlerManager
{
  private static readonly Dictionary<PacketId, Func<uint, IMessage, Task>> _handlers = new();
  private static bool _isInitialized = false;
  private static readonly object _initLock = new object();

  public static void Initialize()
  {
    if (_isInitialized) return;

    lock (_initLock)
    {
      if (_isInitialized) return;

      Console.WriteLine("HandlerManager 초기화 시작...");
      
      // 모든 핸들러 등록
      var authHandler = new AuthPacketHandler();
      authHandler.RegisterHandlers();
      // 다른 핸들러들도 여기서 등록...

      _isInitialized = true;
      Console.WriteLine("HandlerManager 초기화 완료");
    }
  }

  // 핸들러 등록
  public static void RegisterHandler<T>(PacketId packetId, Func<uint, T, Task> handler) where T : IMessage
  {
    _handlers[packetId] = async (seq, message) => await handler(seq, (T)message);
    Console.WriteLine($"핸들러 등록: {typeof(T).Name}");
  }

  // 메시지 처리
  public static async Task HandleMessageAsync(PacketId id, uint sequence, IMessage message)
  {
    if (!_isInitialized)
    {
      throw new InvalidOperationException("HandlerManager가 초기화되지 않았습니다.");
    }

    if (_handlers.TryGetValue(id, out var handler))
    {
      try
      {
        await handler(sequence, message);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"핸들러 에러: {id} / seq:{sequence} - {ex.Message}");
        throw;
      }
    }
    else
    {
      Console.WriteLine($"핸들러 존재 X: {id} / seq:{sequence}");
    }
  }

  // 핸들러 존재 여부 확인
  public static bool HasHandler(PacketId id)
  {
    return _handlers.ContainsKey(id);
  }

  // 특정 메시지 타입 핸들러 제거
  public static void UnregisterHandler(PacketId id)
  {
    if (_handlers.Remove(id))
    {
      Console.WriteLine($"핸들러 제거: {id}");
    }
  }

  // 모든 핸들러 반환
  public static IEnumerable<PacketId> GetRegisteredHandlers()
  {
    return _handlers.Keys;
  }

  // 모든 핸들러 초기화
  public static void ClearHandlers()
  {
    _handlers.Clear();
    Console.WriteLine("모든 핸들러 초기화.");
  }
}