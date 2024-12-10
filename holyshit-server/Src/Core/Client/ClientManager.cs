using System.Net.Sockets;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Core.Client;

public class ClientManager
{
  private static readonly Dictionary<string, ClientSession> _sessions = new(); // 연결된 클라이언트
  private static readonly object _sessionLock = new(); // 클라이언트 목록 동시 접근 제한 객체
  private static IServiceProvider? _serviceProvider;  // DI 컨테이너

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
    Console.WriteLine($"[ClientManager] 새로운 클라이언트 접속: {clientEndPoint}");

    var session = new ClientSession(client, _serviceProvider);

    try
    {
      AddSession(session);
      Console.WriteLine($"[ClientManager] 현재 접속자 수: {GetActiveClientCount()}");
      await session.StartAsync(); // 클라이언트 핸들러 처리 시작
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ClientManager] 클라이언트 처리 중 오류: {ex.Message}");
    }
    finally
    {
      if (session != null)
      {
        RemoveSession(session);
        session.Dispose(); // 클라이언트 핸들러 객체 해제
        Console.WriteLine($"[ClientManager] 클라이언트 접속 종료: {clientEndPoint}");
        Console.WriteLine($"[ClientManager] 남은 접속자 수: {GetActiveClientCount()}");
      }
    }
  }

  public static void AddSession(ClientSession session)
  {
    lock (_sessionLock)
    {
      _sessions[session.SessionId] = session;
      Console.WriteLine($"[ClientManager] 새로운 세션 등록: {session.SessionId}");
    }
  }

  public static void RemoveSession(ClientSession session)
  {
    lock (_sessionLock)
    {
      var uuid = _sessions.FirstOrDefault(x => x.Value == session).Key;
      if (uuid != null)
      {
        _sessions.Remove(uuid);
        Console.WriteLine($"[ClientManager] 세션 제거: UUID={uuid}");
      }
    }
  }

  public static ClientSession? GetSession(string sessionId)
  {
    lock (_sessionLock)
    {
      return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
  }

  public static int GetActiveClientCount()
  {
    lock (_sessionLock)
    {
      return _sessions.Count;
    }
  }

  public static async Task CleanupClientsAsync()
  {
    try
    {
      List<ClientSession> sessionsToDispose;
      lock (_sessionLock)
      {
        sessionsToDispose = _sessions.Values.ToList();
        _sessions.Clear();
      }

      // 각 세션을 비동기적으로 정리
      var disposeTasks = sessionsToDispose.Select(async session =>
      {
        try
        {
          // Dispose 전에 필요한 비동기 작업이 있다면 여기서 처리
          await Task.Run(() => session.Dispose());
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[ClientManager] 세션 정리 중 오류 발생: {session.SessionId}, {ex.Message}");
        }
      });

      // 모든 세션 정리 작업이 완료될 때까지 대기
      await Task.WhenAll(disposeTasks);

      Console.WriteLine("[ClientManager] 모든 세션 정리 완료");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ClientManager] 세션 정리 중 오류: {ex.Message}");
    }
  }
}