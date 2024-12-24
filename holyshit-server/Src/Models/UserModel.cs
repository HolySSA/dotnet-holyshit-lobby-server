using System.Collections.Concurrent;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Models;

public class UserModel
{
  private static UserModel? _instance;
  private static readonly object _lock = new object();

  // 동시성을 고려하여 ConcurrentDictionary 컬렉션 사용
  private readonly ConcurrentDictionary<long, UserInfo> _users = new();

  public static UserModel Instance
  {
    get
    {
      if (_instance == null)
      {
        lock (_lock)
        {
          _instance ??= new UserModel();
        }
      }
      
      return _instance;
    }
  }

  private UserModel() { }

  public class UserInfo
  {
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public UserData UserData { get; set; } = new UserData();
    public ClientSession Client { get; set; }
    public DateTime LastActivityTime { get; set; }
    public bool IsOnline => (DateTime.UtcNow - LastActivityTime).TotalMinutes < 5;

    public UserInfo(long userId, string token, UserData userData, ClientSession client)
    {
      UserId = userId;
      Token = token;
      UserData = userData;
      Client = client;
      LastActivityTime = DateTime.UtcNow;
    }

    public void UpdateActivity()
    {
      LastActivityTime = DateTime.UtcNow;
    }
  }

  public bool AddUser(long userId, string token, UserData userData, ClientSession client)
  {
    var userInfo = new UserInfo(userId, token, userData, client);
    return _users.TryAdd(userId, userInfo);
  }

  public bool RemoveUser(long userId)
  {
    return _users.TryRemove(userId, out _);
  }

  public UserInfo? GetUser(long userId)
  {
    _users.TryGetValue(userId, out var userInfo);
    return userInfo;
  }

  public bool UpdateUserData(long userId, UserData userData)
  {
    if (_users.TryGetValue(userId, out var userInfo))
    {
      userInfo.UserData = userData;
      userInfo.UpdateActivity();

      return true;
    }

    return false;
  }

  public List<UserInfo> GetAllUsers()
  {
    return _users.Values.ToList();
  }

  public List<UserInfo> GetOnlineUsers()
  {
    return _users.Values.Where(u => u.IsOnline).ToList();
  }

/*
    // 특정 방의 모든 유저에게 브로드캐스트
    public async Task BroadcastToRoom(int roomId, PacketId packetId, IMessage message, uint sequence, long? excludeUserId = null)
    {
        var room = RoomModel.Instance.GetRoom(roomId);
        if (room == null) return;

        foreach (var user in room.Users)
        {
            if (excludeUserId.HasValue && user.Id == excludeUserId.Value)
                continue;

            if (_users.TryGetValue(user.Id, out var userInfo))
            {
                await userInfo.Client.SendResponseAsync(packetId, sequence, message);
            }
        }
    }

    // 유저 인증 체크
    public bool ValidateUserToken(long userId, string token)
    {
        if (_users.TryGetValue(userId, out var userInfo))
        {
            return userInfo.Token == token;
        }
        return false;
    }

    // 비활성 유저 정리 (필요시 주기적으로 호출)
    public void CleanupInactiveUsers(int minutesThreshold = 30)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
        var inactiveUsers = _users.Values.Where(u => u.LastActivityTime < threshold).ToList();
        
        foreach (var user in inactiveUsers)
        {
            RemoveUser(user.UserId);
            user.Client.Dispose();
        }
    }
*/
}