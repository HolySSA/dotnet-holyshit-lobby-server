using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services;

public class RoomService : IRoomService
{
  private readonly UserModel _userModel;
  private readonly RoomModel _roomModel;

  public RoomService()
  {
    _userModel = UserModel.Instance;
    _roomModel = RoomModel.Instance;
  }

  /// <summary>
  /// 현재 존재하는 모든 방 목록을 반환
  /// </summary>
  public async Task<ServiceResult<List<RoomData>>> GetRoomList(ClientSession client)
  {
    try
    {
      var roomList = _roomModel.GetRoomList();
      return await Task.FromResult(ServiceResult<List<RoomData>>.Ok(roomList));
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GetRoomList 실패: {ex.Message}");
      return ServiceResult<List<RoomData>>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 생성
  /// </summary>
  public async Task<ServiceResult<RoomData>> CreateRoom(ClientSession client, string name, int maxUserNum)
  {
    try
    {
      return await Task.Run(() =>
      {
        var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
        if (userInfo == null)
          return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

        var existingRoom = _roomModel.GetUserRoom(userInfo.UserId);
        if (existingRoom != null)
          return ServiceResult<RoomData>.Error(GlobalFailCode.CreateRoomFailed);

        if (string.IsNullOrEmpty(name) || maxUserNum < 2 || maxUserNum > 8)
          return ServiceResult<RoomData>.Error(GlobalFailCode.InvalidRequest);

        var room = _roomModel.CreateRoom(name, maxUserNum, userInfo.UserId, userInfo.UserData);
        if (room == null)
          return ServiceResult<RoomData>.Error(GlobalFailCode.CreateRoomFailed);

        return ServiceResult<RoomData>.Ok(room);
      });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] CreateRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 입장
  /// </summary>
  public async Task<ServiceResult<RoomData>> JoinRoom(ClientSession client, int roomId)
  {
    try
    {
      // 유저 정보 확인
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      // 이미 방에 있는지 확인
      var existingRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 요청한 방이 존재하는지 확인
      var targetRoom = _roomModel.GetRoom(roomId);
      if (targetRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      // 인원 수 체크
      if (targetRoom.Users.Count >= targetRoom.MaxUserNum)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 상태 체크
      if (targetRoom.State != RoomStateType.Wait)
        return ServiceResult<RoomData>.Error(GlobalFailCode.InvalidRoomState);

      // 방 입장 처리
      if (!_roomModel.JoinRoom(roomId, userInfo.UserData))
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      var updatedRoom = _roomModel.GetRoom(roomId);
      if (updatedRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      // 다른 유저들에게 알림
      await NotifyRoomMembers(client, updatedRoom, userInfo);

      return ServiceResult<RoomData>.Ok(updatedRoom);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] JoinRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 랜덤 방 입장
  /// </summary>
  public async Task<ServiceResult<RoomData>> JoinRandomRoom(ClientSession client)
  {
    try
    {
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      var existingRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      var availableRooms = _roomModel.GetRoomList()
        .Where(r => r.Users.Count < r.MaxUserNum && r.State == RoomStateType.Wait)
        .ToList();

      if (!availableRooms.Any())
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      var random = new Random();
      var selectedRoom = availableRooms[random.Next(availableRooms.Count)];

      if (!_roomModel.JoinRoom(selectedRoom.Id, userInfo.UserData))
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      var updatedRoom = _roomModel.GetRoom(selectedRoom.Id);
      if (updatedRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      await NotifyRoomMembers(client, updatedRoom, userInfo);
      return ServiceResult<RoomData>.Ok(updatedRoom);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] JoinRandomRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 퇴장
  /// </summary>
  public async Task<ServiceResult> LeaveRoom(ClientSession client)
  {
    try
    {
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      var currentRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      var targetSessionIds = _roomModel.GetRoomTargetSessionIds(currentRoom, userInfo.UserId);

      if (!_roomModel.LeaveRoom(userInfo.UserId))
        return ServiceResult.Error(GlobalFailCode.LeaveRoomFailed);

      if (targetSessionIds.Any())
      {
        var notification = NotificationHelper.CreateLeaveRoomNotification(
          userInfo.UserId,
          targetSessionIds
        );

        await client.MessageQueue.EnqueueSend(
          notification.PacketId,
          notification.Sequence,
          notification.Message,
          notification.TargetSessionIds
        );
      }

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] LeaveRoom 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 레디 토글
  /// </summary>
  public async Task<ServiceResult> GameReady(ClientSession client, bool isReady)
  {
    try
    {
      var (userInfo, room, error) = ValidateUserAndRoom(client);
      if (error != null)
        return error;

      bool toggledReady = !isReady;

      await SendNotificationToRoom(
          client,
          room!,
          0,
          targetSessionIds => NotificationHelper.CreateGameReadyNotification(
            userInfo!.UserId,
            toggledReady,
            targetSessionIds
          )
      );

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GameReady 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 준비 단계 시작
  /// </summary>
  public async Task<ServiceResult> GamePrepare(ClientSession client)
  {
    try
    {
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      var currentRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 이미 게임이 시작된 방인지 체크
      if (currentRoom.State != RoomStateType.Wait)
        return ServiceResult.Error(GlobalFailCode.InvalidRoomState, "이미 게임이 시작되었거나 준비 중인 방입니다.");

      /*
      // 유저의 준비 상태 토글
      if (!_roomModel.ToggleUserReady(currentRoom.Id, userInfo.UserId))
        return ServiceResult.Error(GlobalFailCode.UnknownError);
      */

      // 업데이트된 방 정보 가져오기
      var updatedRoom = _roomModel.GetRoom(currentRoom.Id);
      if (updatedRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 방의 모든 유저에게 알림
      var targetSessionIds = _roomModel.GetRoomTargetSessionIds(updatedRoom, 0);  // 0을 전달하여 모든 유저 포함
      if (targetSessionIds.Any())
      {
        var notification = NotificationHelper.CreateGamePrepareNotification(updatedRoom, targetSessionIds);
        await client.MessageQueue.EnqueueSend(
          notification.PacketId,
          notification.Sequence,
          notification.Message,
          notification.TargetSessionIds
        );
      }

      return ServiceResult.Ok("준비 상태가 변경되었습니다.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GamePrepare 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 시작
  /// </summary>
  public async Task<ServiceResult> GameStart(ClientSession client)
  {
    try
    {
      var (userInfo, room, error) = ValidateUserAndRoom(client);
      if (error != null)
        return error;

      room!.State = RoomStateType.Ingame;

      // 역할 분배
      var roles = GetRoleDistribution(room.Users.Count);
      if (!roles.Any())
        return ServiceResult.Error(GlobalFailCode.InvalidRequest, "유효하지 않은 인원수입니다.");

      // 역할 목록 생성
      var roleList = new List<RoleType>();
      foreach (var (role, count) in roles)
      {
        for (int i = 0; i < count; i++)
        {
          roleList.Add(role);
        }
      }

      // 역할 랜덤 할당
      var random = new Random();
      var shuffledRoles = roleList.OrderBy(_ => random.Next()).ToList();

      // 각 유저에게 역할 할당 및 스탯 설정
      for (int i = 0; i < room.Users.Count; i++)
      {
        var user = room.Users[i];
        var assignedRole = shuffledRoles[i];
        SetInitialStats(user.Character, assignedRole);
        Console.WriteLine($"[GameStart] 유저 {user.Id}에게 역할 {assignedRole} 할당");
      }

      var gameState = new GameStateData
      {
        PhaseType = PhaseType.Day, // 낮부터 시작
        NextPhaseAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds() // 5분 후 다음 페이즈
      };

      var characterPositions = room.Users.Select(user => new CharacterPositionData
      {
        Id = user.Id,
        X = 0,
        Y = 0
      }).ToList();

      await SendNotificationToRoom(
        client,
        room,
        0,
        targetSessionIds => NotificationHelper.CreateGameStartNotification(
          gameState,
          room.Users.ToList(),
          characterPositions,
          targetSessionIds
        )
      );

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GameStart 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 멤버들에게 알림을 전송
  /// </summary>
  private async Task NotifyRoomMembers(ClientSession client, RoomData room, UserModel.UserInfo userInfo)
  {
    var targetSessionIds = _roomModel.GetRoomTargetSessionIds(room, userInfo.UserId);
    if (!targetSessionIds.Any()) return;

    var notification = NotificationHelper.CreateJoinRoomNotification(
      userInfo.UserData,
      targetSessionIds
    );

    await client.MessageQueue.EnqueueSend(
      notification.PacketId,
      notification.Sequence,
      notification.Message,
      notification.TargetSessionIds
    );
  }

  /// <summary>
  /// 유저, 방 정보 검증
  /// </summary>
  private (UserModel.UserInfo? userInfo, RoomData? room, ServiceResult? error) ValidateUserAndRoom(ClientSession client)
  {
    var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
    if (userInfo == null)
      return (null, null, ServiceResult.Error(GlobalFailCode.AuthenticationFailed));

    var currentRoom = _roomModel.GetUserRoom(userInfo.UserId);
    if (currentRoom == null)
      return (userInfo, null, ServiceResult.Error(GlobalFailCode.RoomNotFound));

    return (userInfo, currentRoom, null);
  }

  /// <summary>
  /// 방의 모든 유저에게 알림 전송
  /// </summary>
  /// <param name="excludeUserId">알림을 받지 않을 유저 ID (0이면 모두에게 전송)</param>
  /// <param name="createNotification">알림 메시지 생성 함수</param>
  private async Task SendNotificationToRoom(
    ClientSession client,
    RoomData room,
    long excludeUserId,
    Func<List<string>, GamePacketMessage> createNotification)
  {
    var targetSessionIds = _roomModel.GetRoomTargetSessionIds(room, excludeUserId);
    if (!targetSessionIds.Any()) return;

    var notification = createNotification(targetSessionIds);
    await client.MessageQueue.EnqueueSend(
      notification.PacketId,
      notification.Sequence,
      notification.Message,
      notification.TargetSessionIds
    );
  }

  /// <summary>
  /// 인원 수에 따른 역할 분배 규칙을 반환
  /// </summary>
  private Dictionary<RoleType, int> GetRoleDistribution(int playerCount)
  {
    return playerCount switch
    {
      2 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Hitman, 1 }
        },
      3 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Hitman, 1 },
            { RoleType.Psychopath, 1 }
        },
      4 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Hitman, 2 },
            { RoleType.Psychopath, 1 }
        },
      5 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Bodyguard, 1 },
            { RoleType.Hitman, 2 },
            { RoleType.Psychopath, 1 }
        },
      6 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Bodyguard, 1 },
            { RoleType.Hitman, 3 },
            { RoleType.Psychopath, 1 }
        },
      7 => new Dictionary<RoleType, int>
        {
            { RoleType.Target, 1 },
            { RoleType.Bodyguard, 2 },
            { RoleType.Hitman, 3 },
            { RoleType.Psychopath, 1 }
        },
      _ => new Dictionary<RoleType, int>()
    };
  }

  /// <summary>
  /// 역할에 따른 초기 스탯 설정
  /// </summary>
  private void SetInitialStats(CharacterData character, RoleType role)
  {
    // 기존 캐릭터 데이터 초기화 필요
    character.StateInfo = new CharacterStateInfoData
    {
        State = CharacterStateType.NoneCharacterState,
        NextState = CharacterStateType.NoneCharacterState,
        NextStateAt = 0,
        StateTargetUserId = 0
    };
    
    // 카드 초기화
    character.HandCards.Clear();
    character.Equips.Clear();
    character.Debuffs.Clear();

    character.RoleType = role;
    character.Hp = 3;
    character.BbangCount = 1;
    character.HandCardsCount = 4;
    character.Weapon = 0;

    character.RoleType = role;

    // 기본 스탯
    character.Hp = 3;
    character.BbangCount = 1;
    character.HandCardsCount = 4;

    // 역할별 추가 스탯
    switch (role)
    {
      case RoleType.Target:
        character.Hp = 4;  // 타겟은 체력 +1
        break;
      case RoleType.Bodyguard:
        character.Weapon = 1;  // 보디가드는 기본 무기 장착
        break;
      case RoleType.Hitman:
        character.HandCardsCount = 5;  // 히트맨은 카드 +1
        break;
      case RoleType.Psychopath:
        character.BbangCount = 2;  // 싸이코패스는 빵야 +1
        break;
    }
  }
}