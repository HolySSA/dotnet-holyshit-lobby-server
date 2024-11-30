using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public static class HandlerManager
{
  private static readonly Dictionary<PacketId, Func<IMessage, Task>> _handlers = new();

  // 핸들러 등록
  public static void RegisterHandler<T>(PacketId packetId, Func<uint, T, Task> handler) where T : IMessage
  {
    _handlers[packetId] = async (seq, message) => await handler(seq, (T)message);
    Console.WriteLine($"핸들러 등록: {typeof(T).Name}");
  }

  // 메시지 처리
  public static async Task HandleMessageAsync(PacketId id, uint sequence, IMessage message)
  {
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
      Console.WriteLine($"핸들러 제거: {id} / seq:{sequence}");
    }
  }

  // 모든 핸들러 초기화
  public static void ClearHandlers()
  {
    _handlers.Clear();
    Console.WriteLine("모든 핸들러 초기화.");
  }
}