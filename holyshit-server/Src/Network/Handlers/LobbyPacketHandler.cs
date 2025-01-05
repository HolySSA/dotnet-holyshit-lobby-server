using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Utils.Decode;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Models;

namespace HolyShitServer.Src.Network.Handlers;

public static class LobbyPacketHandler
{
  private static readonly IRoomService _roomService = new RoomService();

  public static async Task<GamePacketMessage> HandleGetRoomListRequest(ClientSession client, uint sequence, C2SGetRoomListRequest request)
  {
    var result = await _roomService.GetRoomList(client.UserId);

    return ResponseHelper.CreateGetRoomListResponse(
      sequence,
      result.Success ? result.Data : new List<RoomData>()
    );
  }

  public static async Task<GamePacketMessage> HandleCreateRoomRequest(ClientSession client, uint sequence, C2SCreateRoomRequest request)
  {
    var result = await _roomService.CreateRoom(userId: client.UserId, name: request.Name, maxUserNum: request.MaxUserNum);
    return ResponseHelper.CreateCreateRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleJoinRoomRequest(ClientSession client, uint sequence, C2SJoinRoomRequest request)
  {
    var result = await _roomService.JoinRoom(client.UserId, request.RoomId);

    if (result.Success && result.Data != null)
    {
      var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(result.Data.Id, client.UserId);
      if (targetSessionIds.Any())
      {
        var userInfo = UserModel.Instance.GetUser(client.UserId);
        if (userInfo?.UserData != null)
        {
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
    }

    return ResponseHelper.CreateJoinRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleJoinRandomRoomRequest(ClientSession client, uint sequence, C2SJoinRandomRoomRequest request)
  {
    var result = await _roomService.JoinRandomRoom(client.UserId);

    // 방 입장 성공 시 방의 모든 유저들에게 알림
    if (result.Success && result.Data != null)
    {
      var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(result.Data.Id, client.UserId);
      if (targetSessionIds.Any())
      {
        var userInfo = UserModel.Instance.GetUser(client.UserId);
        if (userInfo?.UserData != null)
        {
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
    }

    return ResponseHelper.CreateJoinRandomRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleLeaveRoomRequest(ClientSession client, uint sequence, C2SLeaveRoomRequest request)
  {
    // 방 유저들 가져오기
    var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
    var targetSessionIds = currentRoom != null ? 
      RoomModel.Instance.GetRoomTargetSessionIds(currentRoom.Id, client.UserId) : 
      new List<string>();

    // 방 퇴장
    var result = await _roomService.LeaveRoom(client.UserId);

    // 방 퇴장 알림
    if (result.Success && targetSessionIds.Any())
    {
      var notification = NotificationHelper.CreateLeaveRoomNotification(client.UserId, targetSessionIds);
      await client.MessageQueue.EnqueueSend(notification.PacketId, notification.Sequence, notification.Message, notification.TargetSessionIds);
    }

    return ResponseHelper.CreateLeaveRoomResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleGameReadyRequest(ClientSession client, uint sequence, C2SGameReadyRequest request)
  {
    var result = await _roomService.GameReady(client.UserId, request.IsReady);

    // 레디 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(currentRoom.Id, 0);
        if (targetSessionIds.Any())
        {
          var notification = NotificationHelper.CreateGameReadyNotification(
            client.UserId,
            !request.IsReady,
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
    }

    return ResponseHelper.CreateGameReadyResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleGamePrepareRequest(ClientSession client, uint sequence, C2SGamePrepareRequest request)
  {
    // 게임 준비 요청 처리
    var result = await _roomService.GamePrepare(client.UserId);
    
    // 게임 준비 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(currentRoom.Id, 0);
        if (targetSessionIds.Any())
        {
          var notification = NotificationHelper.CreateGamePrepareNotification(
            currentRoom.ToProto(),
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
    }

    return ResponseHelper.CreateGamePrepareResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleGameStartRequest(ClientSession client, uint sequence, C2SGameStartRequest request)
  {
    // 게임 시작 처리
    var result = await _roomService.GameStart(client.UserId);

    // 게임 시작 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(currentRoom.Id, 0);
        if (targetSessionIds.Any())
        {
          // 게임 상태 정보 생성
          var gameState = new GameStateData
          {
            PhaseType = PhaseType.Day, // 낮
            NextPhaseAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds() // 5분 후 다음 페이즈로 이동
          };

          // 유저 정보에 역할 정보 포함
          var users = currentRoom.GetAllUsers();

          // 캐릭터 초기 위치 정보 생성
          var characterPositions = users.Select(user => new CharacterPositionData
          {
            Id = user.Id,
            X = 0,
            Y = 0
          }).ToList();

          var notification = NotificationHelper.CreateGameStartNotification(
            gameState,
            users,
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
      }
    }

    return ResponseHelper.CreateGameStartResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }
}