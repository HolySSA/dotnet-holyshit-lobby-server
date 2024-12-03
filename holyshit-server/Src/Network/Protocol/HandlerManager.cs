using Google.Protobuf;
using HolyShitServer.Src.Network.Handlers;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public static class HandlerManager
{
  private static readonly Dictionary<PacketId, Func<TcpClientHandler, uint, IMessage, Task>> _handlers = new(); // 모든 핸들러
  private static bool _isInitialized = false; // 초기화 여부
  private static readonly object _initLock = new object(); // 초기화/동기화 관리 락 오브젝트

  public static void Initialize()
  {
    if (_isInitialized) return;

    lock (_initLock)
    {
      if (_isInitialized) return;

      Console.WriteLine("HandlerManager 초기화 시작...");
      
      // 모든 핸들러 등록
      OnHandlers<C2SRegisterRequest>(PacketId.C2SregisterRequest, AuthPacketHandler.HandleRegisterRequest);
      OnHandlers<C2SLoginRequest>(PacketId.C2SloginRequest, AuthPacketHandler.HandleLoginRequest);
      // 다른 핸들러들도 여기서 등록...

      _isInitialized = true;
      Console.WriteLine("HandlerManager 초기화 완료");
    }
  }

  // 핸들러 등록
  public static void OnHandlers<T>(PacketId packetId, Func<TcpClientHandler, uint, T, Task> handler) where T : IMessage
  {
    _handlers[packetId] = async (client, seq, message) => await handler(client, seq, (T)message);
    Console.WriteLine($"핸들러 등록: {typeof(T).Name}");
  }

  // 특정 메시지 타입 핸들러 제거
  public static void OffHandlers(PacketId id)
  {
    if (_handlers.Remove(id))
    {
      Console.WriteLine($"핸들러 제거: {id}");
    }
  }

  // 메시지 처리
  public static async Task HandleMessageAsync(TcpClientHandler client, PacketId id, uint sequence, IMessage message)
  {
    if (!_isInitialized)
    {
      throw new InvalidOperationException("HandlerManager가 초기화되지 않았습니다.");
    }

    if (_handlers.TryGetValue(id, out var handler))
    {
      try
      {
        await handler(client, sequence, message);
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