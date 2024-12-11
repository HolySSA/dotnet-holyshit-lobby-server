using System.Collections.Concurrent;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Models;

public class RoomModel
{
  private static RoomModel? _instance;
  private static readonly object _lock = new object();
  private int _nextRoomId = 1;

  // 동시성을 고려하여 ConcurrentDictionary 사용
  private readonly ConcurrentDictionary<int, RoomData> _rooms = new();
  private readonly ConcurrentDictionary<long, int> _userRoomMap = new();

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

  public RoomData? CreateRoom(string name, int maxUserNum, long ownerId, UserData ownerData)
  {
    var roomId = Interlocked.Increment(ref _nextRoomId) - 1;
    var room = new RoomData
    {
      Id = roomId,
      Name = name,
      MaxUserNum = maxUserNum,
      OwnerId = ownerId,
      State = RoomStateType.Wait
    };

    room.Users.Add(ownerData);

    if (_rooms.TryAdd(roomId, room))
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
      if (room.Users.Count >= room.MaxUserNum)
        return false;

      room.Users.Add(userData);
      _userRoomMap.TryAdd(userData.Id, roomId);
      return true;
    }

    return false;
  }

  public bool LeaveRoom(long userId)
  {
    if (_userRoomMap.TryRemove(userId, out var roomId) && _rooms.TryGetValue(roomId, out var room))
    {
      var user = room.Users.FirstOrDefault(u => u.Id == userId);
      if (user != null)
      {
        room.Users.Remove(user);
        
        if (room.Users.Count == 0)
        {
          // 방에 아무도 없으면 방 삭제
          _rooms.TryRemove(roomId, out _);
        }
        else if (room.OwnerId == userId && room.Users.Any())
        {
          // 방장이 나가면 다음 사람에게 방장 위임
          room.OwnerId = room.Users[0].Id;
        }

        return true;
      }
    }
    
    return false;
  }

  public List<RoomData> GetRoomList()
  {
    return _rooms.Values.ToList();
  }

  public RoomData? GetRoom(int roomId)
  {
    _rooms.TryGetValue(roomId, out var room);
    return room;
  }

  public RoomData? GetUserRoom(long userId)
  {
    if (_userRoomMap.TryGetValue(userId, out var roomId))
    {
      return GetRoom(roomId);
    }

    return null;
  }
}