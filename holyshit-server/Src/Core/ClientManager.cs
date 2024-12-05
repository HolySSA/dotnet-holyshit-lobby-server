using System.Net.Sockets;
using HolyShitServer.Src.Network;

public class ClientManager
{
  private static readonly List<TcpClientHandler> _activeClients = new(); // 연결된 클라이언트 목록
  private static readonly object _clientLock = new(); // 클라이언트 목록 동시 접근 제한 객체

  public static void AddClient(TcpClientHandler client)
  {
    lock (_clientLock)
    {
      _activeClients.Add(client);
    }
  }

  public static void RemoveClient(TcpClientHandler client)
  {
    lock (_clientLock)
    {
      _activeClients.Remove(client);
    }
  }

  private static async Task HandleClientAsync(TcpClient client)
  {
    TcpClientHandler? handler = null; // 클라이언트 핸들러 객체 
    var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "알 수 없는 클라이언트";
    Console.WriteLine($"새로운 클라이언트 연결: {clientEndPoint}");

    try
    {
      handler = new TcpClientHandler(client); // 클라이언트 핸들러 객체 생성
      AddClient(handler);
      await handler.StartHandlingClientAsync(); // 클라이언트 핸들러 처리 시작
    }
    catch (Exception ex)
    {
      Console.WriteLine($"클라이언트 처리 중 오류: {ex.Message}");
    }
    finally
    {
      if (handler != null)
      {
        RemoveClient(handler);
        handler.Dispose(); // 클라이언트 핸들러 객체 해제
      }
      
      Console.WriteLine($"클라이언트 연결 종료: {clientEndPoint}");
    }
  }

  private static async Task CleanupAsync()
  {
    try
    {
      // 연결된 클라이언트 정리
      List<TcpClientHandler> clientsToDispose;

      lock (_clientLock)
      {
        clientsToDispose = new List<TcpClientHandler>(_activeClients);
        _activeClients.Clear();
      }

      // 모든 클라이언트 핸들러 비동기로 해제
      var disposeTasks = clientsToDispose.Select(client => Task.Run(() => client.Dispose())).ToList();
      await Task.WhenAll(disposeTasks); // 모든 클라이언트 핸들러 해제 완료 대기

      Console.WriteLine("모든 리소스 정리 완료.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"리소스 정리 중 오류: {ex.Message}");
    }
  }
}