using System.Net.Sockets;
using Google.Protobuf;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Network.Socket;

public class ClientSession : IDisposable
{
  private readonly TcpClient _client; // 현재 연결된 클라이언트
  private readonly NetworkStream _stream; // 클라이언트와의 네트워크 스트림
  private readonly IServiceProvider _serviceProvider;  // DI 컨테이너
  private bool _disposed; // 객체 해제 여부

  public MessageQueue MessageQueue { get; } // 메시지 큐
  public string SessionId { get; }
  public long UserId { get; private set; } // 유저 ID
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
    SessionId = Guid.NewGuid().ToString();
    MessageQueue = new MessageQueue(this);
  }

  /// <summary>
  /// 유저 ID 설정
  /// </summary>
  public void SetUserId(long userId)
  {
    UserId = userId;
  }

  public async Task StartAsync()
  {
    try
    {
      Console.WriteLine($"[Session] 시작: {SessionId}");
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
  public async Task ProcessMessagesAsync()
  {
    while (!_disposed)
    {
      try
      {
        // 헤더 읽기
        var headerBuffer = new byte[PacketSerializer.HEADER_SIZE];
        int headerBytesRead = await _stream.ReadAsync(headerBuffer, 0, PacketSerializer.HEADER_SIZE);
        if (headerBytesRead == 0) break;

        // 페이로드 길이 추출 (헤더의 마지막 4바이트)
        var payloadLength = BitConverter.ToInt32(headerBuffer, 7);

        // 페이로드 읽기
        var payloadBuffer = new byte[payloadLength];
        int payloadBytesRead = await _stream.ReadAsync(payloadBuffer, 0, payloadLength);
        if (payloadBytesRead == 0) break;

        // 전체 패킷 조합
        var packetBuffer = new byte[PacketSerializer.HEADER_SIZE + payloadLength];
        Array.Copy(headerBuffer, 0, packetBuffer, 0, PacketSerializer.HEADER_SIZE);
        Array.Copy(payloadBuffer, 0, packetBuffer, PacketSerializer.HEADER_SIZE, payloadLength);

        // 패킷 처리
        var result = PacketSerializer.Deserialize(packetBuffer);
        if (result.HasValue)
        {
          var (id, sequence, message) = result.Value;
          if (message != null)
          {
            await MessageQueue.EnqueueReceive(id, sequence, message);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Session] 메시지 처리 오류: {SessionId}, {ex.Message}");
        break;
      }
    }
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

  public void Dispose()
  {
    // 객체 해제 여부 확인
    if (_disposed) return;

    try
    {
      var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(user => user.Client == this);
      if (userInfo != null)
      {
        // 방에서 나가기
        var roomModel = RoomModel.Instance;
        var currentRoom = roomModel.GetUserRoom(userInfo.UserId);
        if (currentRoom != null)
        {
          // 방의 다른 유저들에게 알림 보내기
          var targetSessionIds = roomModel.GetRoomTargetSessionIds(currentRoom.Id, userInfo.UserId);
          if (targetSessionIds.Any())
          {
            var notification = NotificationHelper.CreateLeaveRoomNotification(
                userInfo.UserId,
                targetSessionIds
            );

            MessageQueue.EnqueueSend(
              notification.PacketId,
              notification.Sequence,
              notification.Message,
              notification.TargetSessionIds
            ).GetAwaiter().GetResult();
          }

          roomModel.LeaveRoom(userInfo.UserId);
        }

        UserModel.Instance.RemoveUser(userInfo.UserId);
        Console.WriteLine($"[Session] 유저 제거: Id={userInfo.UserId}");
      }

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