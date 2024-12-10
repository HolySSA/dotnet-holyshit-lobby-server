using System.Net.Sockets;
using Google.Protobuf;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Utils.Socket;

namespace HolyShitServer.Src.Network;

public class TcpClientHandler : IDisposable
{
  private readonly TcpClient _client; // 현재 연결된 클라이언트
  private readonly NetworkStream _stream; // 클라이언트와의 네트워크 스트림
  private readonly MessageQueue _messageQueue; // 메시지 큐

  private readonly IServiceProvider _serviceProvider;  // DI 컨테이너
  public IServiceProvider ServiceProvider => _serviceProvider;

  private bool _disposed = false; // 객체 해제 여부

  // 생성자 - 필드 초기화
  public TcpClientHandler(TcpClient client, IServiceProvider serviceProvider)
  {
    _client = client;
    _stream = client.GetStream();
    _serviceProvider = serviceProvider;
    _messageQueue = new MessageQueue(this);
  }

  // 클라이언트 통신 처리 비동기로 시작
  public async Task StartHandlingClientAsync()
  {
    try
    {
      while (!_disposed)
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
            await _messageQueue.EnqueueReceive(id, sequence, message);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"클라이언트 처리 중 오류: {ex.Message}");
    }
  }

  // 응답 전송
  public async Task SendResponseAsync<T>(PacketId packetId, uint sequence, T message) where T : IMessage
  {
    try
    {
      await _messageQueue.EnqueueSend(packetId, sequence, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[TCP] 패킷 전송 중 오류: {ex}");
      throw;
    }
  }

  // MessageQueue에서 사용할 메서드 추가
  public async Task SendDataAsync(byte[] data)
  {
    if (_disposed) throw new ObjectDisposedException(nameof(TcpClientHandler));
    
    await _stream.WriteAsync(data);
    await _stream.FlushAsync();
  }

  public void Dispose()
  {
    // 객체 해제 여부 확인
    if (_disposed) return;

    var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(user => user.Client == this);
    if (userInfo != null)
    {
      UserModel.Instance.RemoveUser(userInfo.UserId);
      Console.WriteLine($"[TCP] 유저 제거: Id={userInfo.UserId}");
    }

    _stream?.Dispose(); // 네트워크 스트림 해제
    _client?.Dispose(); // TcpClient 객체 해제
    _disposed = true;
  }
}