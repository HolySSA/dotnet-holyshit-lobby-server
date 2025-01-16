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
  private readonly ConcurrentDictionary<int, UserInfo> _users = new();

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
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public UserData UserData { get; set; } = new UserData();
    public ClientSession Client { get; set; }
    public DateTime LastActivityTime { get; set; }
    public bool IsOnline => (DateTime.UtcNow - LastActivityTime).TotalMinutes < 5;

    public UserInfo(int userId, string token, UserData userData, ClientSession client)
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

  public bool AddUser(int userId, string token, UserData userData, ClientSession client)
  {
    var userInfo = new UserInfo(userId, token, userData, client);
    return _users.TryAdd(userId, userInfo);
  }

  public bool RemoveUser(int userId)
  {
    return _users.TryRemove(userId, out _);
  }

  public UserInfo? GetUser(int userId)
  {
    _users.TryGetValue(userId, out var userInfo);
    return userInfo;
  }

  public bool UpdateUserData(int userId, UserData userData)
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
}