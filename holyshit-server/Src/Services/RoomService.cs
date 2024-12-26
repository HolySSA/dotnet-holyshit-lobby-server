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

  public async Task<ServiceResult> GameReady(ClientSession client, bool isReady)
  {
    try
    {
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      var currentRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 현재 유저 찾기
      var user = currentRoom.Users.FirstOrDefault(u => u.Id == userInfo.UserId);
      
      // 토글된 상태 (클라이언트가 보낸 값의 반대)
      bool toggledReady = !isReady;

      // 방의 모든 유저에게 알림
      var targetSessionIds = _roomModel.GetRoomTargetSessionIds(currentRoom, 0);
      if (targetSessionIds.Any())
      {
        var notification = NotificationHelper.CreateGameReadyNotification(
          userInfo.UserId,
          toggledReady,
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
      Console.WriteLine($"[RoomService] GameReady 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

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

  public async Task<ServiceResult> GameStart(ClientSession client)
  {
    try
    {
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      var currentRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 방 상태 변경
      currentRoom.State = RoomStateType.Ingame;

      // 게임 상태 초기화
      var gameState = new GameStateData
      {
        PhaseType = PhaseType.Day,  // 낮부터 시작
        NextPhaseAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds()  // 5분 후 다음 페이즈
      };

      // 캐릭터 위치 정보 초기화
      var characterPositions = currentRoom.Users.Select(user => new CharacterPositionData
      {
        Id = user.Id,
        X = 0,  // 시작 위치 X
        Y = 0   // 시작 위치 Y
      }).ToList();

      // 방의 모든 유저에게 알림
      var targetSessionIds = _roomModel.GetRoomTargetSessionIds(currentRoom, 0);
      if (targetSessionIds.Any())
      {
        var notification = NotificationHelper.CreateGameStartNotification(
          gameState,
          currentRoom.Users.ToList(),
          characterPositions,
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
      Console.WriteLine($"[RoomService] GameStart 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

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
}