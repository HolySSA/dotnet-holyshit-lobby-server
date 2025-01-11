using System.Collections.Concurrent;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Models;


public struct Vector2
{
  public double X { get; set; }
  public double Y { get; set; }

  public Vector2(double x, double y)
  {
    X = x;
    Y = y;
  }
}

public class RoomModel
{
  private static RoomModel? _instance;
  private static readonly object _lock = new object();
  private int _nextRoomId = 1;

  // 동시성을 고려하여 ConcurrentDictionary 컬렉션 사용
  private readonly ConcurrentDictionary<int, Room> _rooms = new();
  private readonly ConcurrentDictionary<int, int> _userRoomMap = new();

  // 스폰 포인트
  private readonly List<Vector2> _spawnPoints = new()
  {
    new Vector2(-3.972, 3.703),
    new Vector2(10.897, 4.033),
    new Vector2(11.737, -5.216),
    new Vector2(5.647, -5.126),
    new Vector2(-6.202, -5.126),
    new Vector2(-13.262, 4.213),
    new Vector2(-22.742, 3.653),
    new Vector2(-21.622, -6.936),
    new Vector2(-124.732, -6.886),
    new Vector2(-15.702, 6.863),
    new Vector2(-1.562, 6.173),
    new Vector2(-13.857, 6.073),
    new Vector2(5.507, 11.963),
    new Vector2(-18.252, 12.453),
    new Vector2(-1.752, -7.376),
    new Vector2(21.517, -4.826),
    new Vector2(21.717, 3.223),
    new Vector2(23.877, 10.683),
    new Vector2(15.337, -12.296),
    new Vector2(-15.202, -4.736),
  };
  
  public static RoomModel Instance
  {
    get
    {
      if (_instance == null)
      {
        lock (_lock)
        {
          _instance ??= new RoomModel();
        }
      }

      return _instance;
    }
  }

  private RoomModel() { }

  public Room? CreateRoom(string name, int maxUserNum, int ownerId, UserData ownerData)
  {
    var roomId = Interlocked.Increment(ref _nextRoomId) - 1;
    var room = new Room
    {
      Id = roomId,
      Name = name,
      MaxUserNum = maxUserNum,
      OwnerId = ownerId,
      State = RoomStateType.Wait
    };

    if (room.AddUser(ownerData) && _rooms.TryAdd(roomId, room))
    {
      _userRoomMap.TryAdd(ownerId, roomId);
      return room;
    }

    return null;
  }

  public bool JoinRoom(int roomId, UserData userData)
  {
    if (_rooms.TryGetValue(roomId, out var room))
    {
      if (room.GetAllUsers().Count >= room.MaxUserNum)
        return false;

      if (room.AddUser(userData))
      {
        _userRoomMap.TryAdd(userData.Id, roomId);
        return true;
      }
    }
    return false;
  }

  public bool LeaveRoom(int userId)
  {
    if (_userRoomMap.TryRemove(userId, out var roomId) && _rooms.TryGetValue(roomId, out var room))
    {
      room.RemoveUser(userId);

      if (room.GetAllUsers().Count == 0)
        _rooms.TryRemove(roomId, out _);
      else if (room.OwnerId == userId && room.GetAllUsers().Any())
        room.OwnerId = room.GetAllUsers()[0].Id;

      return true;
    }
    return false;
  }

  public List<RoomData> GetRoomList()
  {
    return _rooms.Values.Select(r => r.ToProto()).ToList();
  }

  public Room? GetRoom(int roomId)
  {
    _rooms.TryGetValue(roomId, out var room);
    return room;
  }

  public Room? GetUserRoom(int userId)
  {
    if (_userRoomMap.TryGetValue(userId, out var roomId))
    {
      return GetRoom(roomId);
    }
    return null;
  }

  public List<int> GetRoomTargetUserIds(int roomId, int excludeUserId)
  {
    var room = GetRoom(roomId);
    if (room == null)
      return new List<int>();

    return room.GetAllUsers()
      .Where(u => u.Id != excludeUserId)
      .Select(u => u.Id)
      .ToList();
  }

  public bool SetRoomState(int roomId, RoomStateType newState)
  {
    if (_rooms.TryGetValue(roomId, out var room))
    {
      return room.SetState(newState);
    }

    return false;
  }

  public List<RoomUserReadyData> GetRoomReadyStates(int roomId)
  {
    if (_rooms.TryGetValue(roomId, out var room))
    {
      return room.GetAllReadyStates();
    }

    return new List<RoomUserReadyData>();
  }

  public List<Vector2> GetRandomSpawnPoints(int count)
  {
    if (count <= 0 || count > _spawnPoints.Count)
      return new List<Vector2>();

    // 스폰 포인트 리스트를 섞어서 랜덤하게 선택
    var random = new Random();
    return _spawnPoints.OrderBy(x => random.Next()).Take(count).ToList();
  }
}