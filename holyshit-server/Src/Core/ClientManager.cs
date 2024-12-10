using System.Net.Sockets;
using HolyShitServer.Src.Network;

namespace HolyShitServer.Src.Core;

public class ClientManager
{
  private static readonly Dictionary<string, TcpClientHandler> _clients = new(); // 연결된 클라이언트
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
    var uuid = Guid.NewGuid().ToString(); // UUID 생성

    try
    {
      AddClient(handler, uuid);
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

  public static void AddClient(TcpClientHandler client, string uuid)
  {
    lock (_clientLock)
    {
      if (_clients.TryGetValue(uuid, out var existingClient))
      {
        existingClient.Dispose();
        Console.WriteLine($"[Client] 기존 연결 종료: UUID={uuid}");
      }

      _clients[uuid] = client;
      Console.WriteLine($"[Client] 새로운 클라이언트 등록: UUID={uuid}");
    }
  }

  public static void RemoveClient(TcpClientHandler client)
  {
    lock (_clientLock)
    {
      var uuid = _clients.FirstOrDefault(x => x.Value == client).Key;
      if (uuid != null)
      {
        _clients.Remove(uuid);
        Console.WriteLine($"[Client] 클라이언트 제거: UUID={uuid}");
      }
    }
  }

  public static TcpClientHandler? GetClientByUUID(string uuid)
  {
    lock (_clientLock)
    {
      return _clients.TryGetValue(uuid, out var client) ? client : null;
    }
  }

  public static int GetActiveClientCount()
  {
    lock (_clientLock)
    {
      return _clients.Count;
    }
  }

  public static async Task CleanupClientsAsync()
  {
    try
    {
      List<TcpClientHandler> clientsToDispose;
      lock (_clientLock)
      {
        clientsToDispose = _clients.Values.ToList();
        _clients.Clear();
      }

      var disposeTasks = clientsToDispose.Select(client => Task.Run(() => client.Dispose()));
      await Task.WhenAll(disposeTasks);

      Console.WriteLine("모든 리소스 정리 완료.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"리소스 정리 중 오류: {ex.Message}");
    }
  }
}