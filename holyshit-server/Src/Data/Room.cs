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

  // 입장 순서를 저장하는 리스트 추가
  private readonly List<int> _joinOrder = new();
  private readonly object _joinOrderLock = new();  // 리스트 동시성 제어용

  // 유저 관리 메서드들
  public bool AddUser(UserData userData)
  {
    if (_users.TryAdd(userData.Id, userData))
    {
      lock (_joinOrderLock)
      {
        _joinOrder.Add(userData.Id);  // 입장 순서 저장
      }
      _userReadyStates.TryAdd(userData.Id, false); // 입장 유저 레디 false로 초기화
      return true;
    }

    return false;
  }

  public bool RemoveUser(int userId)
  {
    lock (_joinOrderLock)
    {
      _joinOrder.Remove(userId);
    }
    _userReadyStates.TryRemove(userId, out _);
    return _users.TryRemove(userId, out _);
  }

  public UserData? GetUser(int userId)
  {
    _users.TryGetValue(userId, out var userData);
    return userData;
  }

  /// <summary>
  /// 방 유저 목록 조회
  /// </summary>
  public List<UserData> GetAllUsers()
  {
    lock (_joinOrderLock)
    {
      // 입장 순서대로 유저 반환
      return _joinOrder.Select(id => _users[id]).ToList();
    }
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

  /// <summary>
  /// Proto 메시지로 변환하는 메서드
  /// </summary>
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