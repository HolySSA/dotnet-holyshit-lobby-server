using System.Collections.Concurrent;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Models;

public class RoomModel
{
  private static RoomModel? _instance;
  private static readonly object _lock = new object();
  private int _nextRoomId = 1;

  // 동시성을 고려하여 ConcurrentDictionary 컬렉션 사용
  private readonly ConcurrentDictionary<int, Room> _rooms = new();
  private readonly ConcurrentDictionary<int, int> _userRoomMap = new();

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

  /*
    // 방의 모든 유저가 준비되었는지 확인하는 메서드 추가
    public bool AreAllUsersReady(int roomId)
    {
      if (!_rooms.TryGetValue(roomId, out var room))
        return false;


      // 방장을 제외한 모든 유저가 준비 상태인지 확인
      return room.Users
          .Where(u => u.Id != room.OwnerId)
          .All(u => u.Character.StateInfo.State == CharacterStateType.Wait);
    }
    */
}