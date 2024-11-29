using System.Net.Sockets;
using System.Text;

namespace HolyShitServer.Src.Network;

public class TcpClientHandler : IDisposable
{
  private readonly TcpClient _client; // 현재 연결된 클라이언트
  private bool _disposed = false; // 객체 해제 여부
  public event Action<string>? onDataReceived; // 클라이언트로부터 데이터를 수신했을 때 실행될 콜백 이벤트

  // 생성자 - 필드 초기화
  public TcpClientHandler(TcpClient client)
  {
    _client = client;
  }

  // 클라이언트 통신 처리 비동기로 시작
  public async Task StartHandlingClientAsync()
  {
    // 네트워크 스트림 - 데이터 read / write 시 필요
    using var stream = _client.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8);
    using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true }; // AutoFlush로 매번 즉시 전송

    while (!_disposed)
    {
      try
      {
        string? data = await reader.ReadLineAsync(); // 데이터를 한 줄 씩 비동기로 읽기
        if (data == null) break;

        onDataReceived?.Invoke(data); // 데이터 수신 이벤트 - 데이터 전달
        await writer.WriteLineAsync($"Server Received: {data}"); // 클라이언트에게 응답
      }
      catch (Exception ex)
      {
        Console.WriteLine($"클라이언트 처리 중 오류: {ex.Message}");
      }
    }
  }

  public void Dispose()
  {
    // 객체 해제 여부 확인
    if (_disposed) return;

    _client.Dispose(); // TcpClient 객체 해제
    _disposed = true;
  }
}