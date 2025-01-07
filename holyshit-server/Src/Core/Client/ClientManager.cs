using System.Collections.Concurrent;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Core.Client;

public class ClientManager
{
  private static readonly ConcurrentDictionary<int, ClientSession> _userSessions = new();
  private static readonly ConcurrentDictionary<string, ClientSession> _sessions = new();

  private static readonly object _sessionLock = new(); // 클라이언트 목록 동시 접근 제한 객체

  public void AddSession(ClientSession session)
  {
    _sessions.TryAdd(session.SessionId, session);
    Console.WriteLine($"[ClientManager] 세션 등록: SessionId={session.SessionId}");
  }

  public void RegisterUserSession(int userId, ClientSession session)
  {
    _userSessions.TryAdd(userId, session);
    Console.WriteLine($"[ClientManager] 유저 세션 등록: UserId={userId}, SessionId={session.SessionId}");
  }

  public ClientSession? GetSessionByUserId(int userId)
  {
    _userSessions.TryGetValue(userId, out var session);
    if (session == null)
    {
      Console.WriteLine($"[ClientManager] 유저 세션 찾기 실패: UserId={userId}");
    }
    return session;
  }

  public void RemoveSession(ClientSession session)
  {
    _sessions.TryRemove(session.SessionId, out _);
    if (session.UserId > 0)
    {
      _userSessions.TryRemove(session.UserId, out _);
      Console.WriteLine($"[ClientManager] 세션 제거: UserId={session.UserId}, SessionId={session.SessionId}");
    }
  }

  public int GetActiveSessionCount()
  {
    return _sessions.Count;
  }

  public IEnumerable<ClientSession> GetActiveSessions()
  {
    return _sessions.Values;
  }
}