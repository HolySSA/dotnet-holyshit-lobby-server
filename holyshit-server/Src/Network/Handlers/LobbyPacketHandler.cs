using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Utils.Decode;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Models;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.DB.Contexts;

namespace HolyShitServer.Src.Network.Handlers;

public static class LobbyPacketHandler
{
  public static async Task<GamePacketMessage> HandleGetRoomListRequest(ClientSession client, uint sequence, C2SGetRoomListRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    var result = await roomService.GetRoomList();

    return ResponseHelper.CreateGetRoomListResponse(
      sequence,
      result.Success ? result.Data : new List<RoomData>()
    );
  }

  public static async Task<GamePacketMessage> HandleCreateRoomRequest(ClientSession client, uint sequence, C2SCreateRoomRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    var result = await roomService.CreateRoom(client.UserId, request.Name, request.MaxUserNum);
    return ResponseHelper.CreateCreateRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  /// <summary>
  /// 방 입장 요청 처리
  /// </summary>
  public static async Task<GamePacketMessage> HandleJoinRoomRequest(ClientSession client, uint sequence, C2SJoinRoomRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    // 방 입장 처리
    var result = await roomService.JoinRoom(client.UserId, request.RoomId);
    // 방 입장 알림
    if (result.Success && result.Data != null)
    {
      var targetUserIds = RoomModel.Instance.GetRoomTargetUserIds(result.Data.Id, client.UserId);
      if (targetUserIds.Any())
      {
        var joinedUser = result.Data.Users.FirstOrDefault(u => u.Id == client.UserId);
        if (joinedUser != null)
        {
          // 알림 생성 및 브로드캐스트
          var notification = NotificationHelper.CreateJoinRoomNotification(joinedUser, targetUserIds);
          var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
          await messageQueueService.BroadcastMessage(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            targetUserIds
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
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    var result = await roomService.JoinRandomRoom(client.UserId);

    // 방 입장 성공 시 방의 모든 유저들에게 알림
    if (result.Success && result.Data != null)
    {
      var targetUserIds = RoomModel.Instance.GetRoomTargetUserIds(result.Data.Id, client.UserId);
      if (targetUserIds.Any())
      {
        var joinedUser = result.Data.Users.FirstOrDefault(u => u.Id == client.UserId);
        if (joinedUser != null)
        {
          // 알림 생성 및 브로드캐스트
          var notification = NotificationHelper.CreateJoinRoomNotification(joinedUser, targetUserIds);
          var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
          await messageQueueService.BroadcastMessage(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            targetUserIds
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
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    // 방 유저들 가져오기
    var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
    var wasOwner = currentRoom?.OwnerId == client.UserId;

    // 방 퇴장
    var result = await roomService.LeaveRoom(client.UserId);
    // 방 퇴장 알림
    if (result.Success)
    {
      var targetUserIds = currentRoom != null ? RoomModel.Instance.GetRoomTargetUserIds(currentRoom.Id, client.UserId) : new List<int>();
      if (currentRoom != null && targetUserIds.Any())
      {
        var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
        var notification = NotificationHelper.CreateLeaveRoomNotification(
          client.UserId,
          wasOwner ? currentRoom.OwnerId : 0,
          targetUserIds
        );
        await messageQueueService.BroadcastMessage(
          notification.PacketId,
          notification.Sequence,
          notification.Message,
          targetUserIds
        );
      }
    }

    return ResponseHelper.CreateLeaveRoomResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleRoomReadyRequest(ClientSession client, uint sequence, C2SRoomReadyRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    var result = await roomService.RoomReady(client.UserId, request.IsReady);

    // 레디 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetUserIds = RoomModel.Instance.GetRoomTargetUserIds(currentRoom.Id, client.UserId);
        if (targetUserIds.Any())
        {
          var notification = NotificationHelper.CreateRoomReadyNotification(
            client.UserId,
            request.IsReady,
            targetUserIds
          );
          var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
          await messageQueueService.BroadcastMessage(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            targetUserIds
          );
        }
      }
    }

    return ResponseHelper.CreateRoomReadyResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  /// <summary>
  /// 방 레디 상태 요청 처리
  /// </summary>
  public static async Task<GamePacketMessage> HandleGetRoomReadyStateRequest(ClientSession client, uint sequence, C2SGetRoomReadyStateRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    var result = await roomService.GetRoomReadyState(request.RoomId);
    return ResponseHelper.CreateGetRoomReadyStateResponse(
      sequence,
      result.Success,
      result.Data ?? new List<RoomUserReadyData>(),
      result.FailCode
    );
  }

  /*
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
  */
}