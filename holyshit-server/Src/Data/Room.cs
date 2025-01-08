using System.Collections.Concurrent;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Data;

public class Room
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public int MaxUserNum { get; set; }
  public int OwnerId { get; set; }
  public RoomStateType State { get; set; }

  // 동시성을 고려한 컬렉션들
  private readonly ConcurrentDictionary<int, UserData> _users = new();
  private readonly ConcurrentDictionary<int, bool> _userReadyStates = new();
  private readonly ConcurrentDictionary<int, CharacterPositionData> _characterPositions = new();

  // 유저 관리 메서드들
  public bool AddUser(UserData userData)
  {
    if (_users.TryAdd(userData.Id, userData))
    {
      _userReadyStates.TryAdd(userData.Id, false); // false로 초기화
      return true;
    }

    return false;
  }

  public bool RemoveUser(int userId)
  {
    _userReadyStates.TryRemove(userId, out _);
    return _users.TryRemove(userId, out _);
  }

  public UserData? GetUser(int userId)
  {
    _users.TryGetValue(userId, out var userData);
    return userData;
  }

  public List<UserData> GetAllUsers()
  {
    return _users.Values.ToList();
  }

  // 방 상태 변경 메서드 추가
  public bool SetState(RoomStateType newState)
  {
    try
    {
      State = newState;
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Room] SetState 실패: {ex.Message}");
      return false;
    }
  }

  // 레디 상태 관리 메서드들
  public bool SetUserReady(int userId, bool isReady)
  {
    try
    {
      _userReadyStates.AddOrUpdate(userId, isReady, (_, _) => isReady);
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Room] SetUserReady 실패: {ex.Message}");
      return false;
    }
  }

  public bool GetUserReady(int userId)
  {
    return _userReadyStates.TryGetValue(userId, out var isReady) && isReady;
  }

  public List<RoomUserReadyData> GetAllReadyStates()
  {
    return _userReadyStates.Select(kvp => new RoomUserReadyData 
    { 
      UserId = kvp.Key, 
      IsReady = kvp.Value 
    })
    .ToList();
  }

  // 위치 관리 메서드들
  public bool UpdatePosition(int userId, double x, double y)
  {
    try
    {
      _characterPositions.AddOrUpdate(
        userId,
        new CharacterPositionData { Id = userId, X = x, Y = y },
        (_, existing) =>
        {
          existing.X = x;
          existing.Y = y;
          return existing;
        }
      );
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Room] UpdatePosition 실패: {ex.Message}");
      return false;
    }
  }

  public List<CharacterPositionData> GetAllPositions()
  {
    return _characterPositions.Values.ToList();
  }

  // Proto 메시지로 변환하는 메서드
  public RoomData ToProto()
  {
    var roomData = new RoomData
    {
      Id = Id,
      Name = Name,
      MaxUserNum = MaxUserNum,
      OwnerId = OwnerId,
      State = State
    };

    roomData.Users.AddRange(GetAllUsers());
    return roomData;
  }
}