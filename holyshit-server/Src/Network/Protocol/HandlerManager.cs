using Google.Protobuf;
using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Network.Handlers;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Network.Protocol;

public static class HandlerManager
{
  private static IServiceProvider? _serviceProvider;
  private static readonly Dictionary<PacketId, Func<ClientSession, uint, IMessage, Task<GamePacketMessage>>> _handlers = new(); // 모든 핸들러
  private static bool _isInitialized = false; // 초기화 여부
  private static readonly object _initLock = new object(); // 초기화/동기화 관리 락 오브젝트

  public static void Initialize(IServiceProvider serviceProvider)
  {
    if (_isInitialized) return;

    lock (_initLock)
    {
      if (_isInitialized) return;

      _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

      // 모든 핸들러 등록
      RegisterHandlers();

      _isInitialized = true;
      Console.WriteLine("HandlerManager 초기화 완료");
    }
  }

  private static void RegisterHandlers()
  {
    //OnHandlers<C2SLoginRequest>(PacketId.LoginRequest, AuthPacketHandler.HandleLoginRequest);
    OnHandlers<C2SGetRoomListRequest>(PacketId.GetRoomListRequest, LobbyPacketHandler.HandleGetRoomListRequest);
    OnHandlers<C2SCreateRoomRequest>(PacketId.CreateRoomRequest, LobbyPacketHandler.HandleCreateRoomRequest);
    OnHandlers<C2SJoinRoomRequest>(PacketId.JoinRoomRequest, LobbyPacketHandler.HandleJoinRoomRequest);
    OnHandlers<C2SJoinRandomRoomRequest>(PacketId.JoinRandomRoomRequest, LobbyPacketHandler.HandleJoinRandomRoomRequest);
    OnHandlers<C2SLeaveRoomRequest>(PacketId.LeaveRoomRequest, LobbyPacketHandler.HandleLeaveRoomRequest);
    OnHandlers<C2SGameReadyRequest>(PacketId.GameReadyRequest, LobbyPacketHandler.HandleGameReadyRequest);
    OnHandlers<C2SGamePrepareRequest>(PacketId.GamePrepareRequest, LobbyPacketHandler.HandleGamePrepareRequest);
    OnHandlers<C2SGameStartRequest>(PacketId.GameStartRequest, LobbyPacketHandler.HandleGameStartRequest);
    OnHandlers<C2SPositionUpdateRequest>(PacketId.PositionUpdateRequest, GamePacketHandler.HandlePositionUpdateRequest);
    // 다른 핸들러들도 여기서 등록...
  }

  // 핸들러 등록
  public static void OnHandlers<T>(
        PacketId packetId,
        Func<ClientSession, uint, T, Task<GamePacketMessage>> handler) where T : IMessage
  {
    _handlers[packetId] = async (client, seq, message) => await handler(client, seq, (T)message);
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
  public static async Task HandleMessageAsync(ClientSession client, PacketId id, uint sequence, IMessage message)
  {
    if (!_isInitialized || _serviceProvider == null)
      throw new InvalidOperationException("HandlerManager가 초기화되지 않았습니다.");

    using var scope = _serviceProvider.CreateScope();
    if (_handlers.TryGetValue(id, out var handler))
    {
      try
      {
        client.ServiceScope = scope; // 클라이언트 세션에 현재 스코프 저장
        var result = await handler(client, sequence, message);
        if (result != null)
        {
          // 핸들러 결과 메시지 큐에 자동으로 추가
          await client.MessageQueue.EnqueueSend(
            result.PacketId,
            result.Sequence,
            result.Message,
            result.TargetSessionIds);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"핸들러 에러: {id} / seq:{sequence} - {ex.Message}");
        throw;
      }
      finally
      {
        client.ServiceScope = null;
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