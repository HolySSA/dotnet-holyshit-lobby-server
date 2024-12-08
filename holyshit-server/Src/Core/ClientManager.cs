using System.Net.Sockets;
using HolyShitServer.Src.Network;

namespace HolyShitServer.Src.Core;

public class ClientManager
{
  private static readonly List<TcpClientHandler> _activeClients = new(); // 연결된 클라이언트 목록
  private static readonly object _clientLock = new(); // 클라이언트 목록 동시 접근 제한 객체
  private static IServiceProvider? _serviceProvider;  // DI 컨테이너

  // 초기화
  public static void Initialize(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public static async Task HandleClientAsync(TcpClient client)
  {
    if (_serviceProvider == null)
    {
      throw new InvalidOperationException("ClientManager가 초기화되지 않았습니다.");
    }

    var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    Console.WriteLine($"[Client] 새로운 클라이언트 접속: {clientEndPoint}");

    var handler = new TcpClientHandler(client, _serviceProvider);

    try
    {
      AddClient(handler);
      Console.WriteLine($"[Client] 현재 접속자 수: {GetActiveClientCount()}");
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
        Console.WriteLine($"[Client] 클라이언트 접속 종료: {clientEndPoint}");
        Console.WriteLine($"[Client] 남은 접속자 수: {GetActiveClientCount()}");
      }
    }
  }

  private static int GetActiveClientCount()
  {
    lock (_clientLock)
    {
      return _activeClients.Count;
    }
  }

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

  public static async Task CleanupClientsAsync()
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