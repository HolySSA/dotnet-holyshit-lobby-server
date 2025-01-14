using System.Net.Sockets;
using Google.Protobuf;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Models;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.Src.Services;
using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Core.Client;

namespace HolyShitServer.Src.Network.Socket;

public class ClientSession : IDisposable
{
  private readonly TcpClient _client; // 현재 연결된 클라이언트
  private readonly NetworkStream _stream; // 클라이언트와의 네트워크 스트림
  private readonly IServiceProvider _serviceProvider;  // DI 컨테이너
  private readonly ClientManager _clientManager;
  private bool _disposed; // 객체 해제 여부
  private bool _isGameStarted; // 게임 시작 플래그

  public MessageQueue MessageQueue { get; } // 메시지 큐
  public string SessionId { get; }
  public int UserId { get; private set; } // 유저 ID
  public IServiceProvider ServiceProvider => _serviceProvider;
  public IServiceScope? ServiceScope { get; set; }

  /// <summary>
  /// 생성자 - 필드 초기화
  /// </summary>
  public ClientSession(TcpClient client, IServiceProvider serviceProvider)
  {
    _client = client;
    _stream = client.GetStream();
    _serviceProvider = serviceProvider;
    _clientManager = serviceProvider.GetRequiredService<ClientManager>();
    SessionId = Guid.NewGuid().ToString();
    MessageQueue = new MessageQueue(this);

    // 세션 등록
    _clientManager.AddSession(this);
  }

  /// <summary>
  /// 유저 ID 설정
  /// </summary>
  public void SetUserId(int userId)
  {
    UserId = userId;

    // 유저 ID로 세션 등록
    _clientManager.RegisterUserSession(userId, this);
  }

  public async Task StartAsync()
  {
    try
    {
      await ProcessMessagesAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Session] 오류 발생: {SessionId}, {ex.Message}");
    }
    finally
    {
      Dispose();
    }
  }


  // 클라이언트 통신 처리 비동기로 시작
  private async Task ProcessMessagesAsync()
  {
    var buffer = new byte[8192];
    var messageBuffer = new List<byte>();

    while (!_disposed)
    {
      try
      {
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0)
        {
          Console.WriteLine("[Session] 클라이언트 연결 종료");
          break;
        }

        messageBuffer.AddRange(buffer.Take(bytesRead));

        // 완전한 패킷이 도착할 때까지 계속 처리
        while (messageBuffer.Count >= PacketSerializer.HEADER_SIZE)
        {
          var currentBuffer = messageBuffer.ToArray();
          var result = PacketSerializer.GetExpectedPacketSize(currentBuffer);

          if (!result.isValid)
          {
            messageBuffer.RemoveAt(0);
            continue;
          }

          var expectedSize = result.totalSize;
          if (expectedSize == 0 || messageBuffer.Count < expectedSize)
            break;

          // 완전한 패킷 처리
          var packetData = messageBuffer.Take(expectedSize).ToArray();
          await ProcessPacketAsync(packetData);
          messageBuffer.RemoveRange(0, expectedSize);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Session] 오류 발생: {SessionId}, {ex.Message}");
        break;
      }
    }
  }

  private async Task ProcessPacketAsync(byte[] packetData)
  {
    var result = PacketSerializer.Deserialize(packetData);
    if (!result.HasValue) return;

    var (id, seq, message) = result.Value;
    if (message == null) return;

    await MessageQueue.EnqueueReceive(id, seq, message);
  }

  // 응답 전송
  public async Task SendMessageAsync<T>(PacketId packetId, uint sequence, T message) where T : IMessage
  {
    try
    {
      await MessageQueue.EnqueueSend(packetId, sequence, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Session] 메시지 전송 오류: {SessionId}, {ex.Message}");
      throw;
    }
  }

  // MessageQueue에서 사용할 메서드 추가
  public async Task SendDataAsync(byte[] data)
  {
    if (_disposed) throw new ObjectDisposedException(nameof(ClientSession));

    await _stream.WriteAsync(data);
    await _stream.FlushAsync();
  }

  /// <summary>
  /// 게임 시작 시 플래그 설정
  /// </summary>
  public void SetGameStarted()
  {
    _isGameStarted = true;
  }

  public void Dispose()
  {
    // 객체 해제 여부 확인
    if (_disposed) return;

    try
    {
      if (!_isGameStarted && UserId > 0)
      {
        using var scope = ServiceProvider.CreateScope();
        var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Redis 캐릭터 정보 DB에 동기화
        redisService.SyncSelectedCharacterToDbAsync(UserId, dbContext).GetAwaiter().GetResult();
        // Redis 유저 관련 데이터 정리
        redisService.ClearUserDataAsync(UserId).GetAwaiter().GetResult();

        // 방에서 나가기
        var roomModel = RoomModel.Instance;
        var currentRoom = roomModel.GetUserRoom(UserId);
        var wasOwner = currentRoom?.OwnerId == UserId;
        if (currentRoom != null)
        {
          // 방의 다른 유저들에게 나가기 알림 보내기
          var targetUserIds = roomModel.GetRoomTargetUserIds(currentRoom.Id, UserId);
          if (targetUserIds.Any())
          {
            var notification = NotificationHelper.CreateLeaveRoomNotification(
              UserId,
              wasOwner ? currentRoom.OwnerId : 0,
              targetUserIds
            );
            var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
            messageQueueService.BroadcastMessage(
              notification.PacketId,
              notification.Sequence,
              notification.Message,
              targetUserIds
            ).GetAwaiter().GetResult();
          }

          // 방 나가기 처리
          roomModel.LeaveRoom(UserId);
        }
      }

      // 세션 제거
      _clientManager.RemoveSession(this);
      _stream?.Dispose();
      _client?.Dispose();
      _disposed = true;

      Console.WriteLine($"[Session] 종료: {SessionId}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Session] Dispose 중 오류 발생: {ex.Message}");
    }
  }
}