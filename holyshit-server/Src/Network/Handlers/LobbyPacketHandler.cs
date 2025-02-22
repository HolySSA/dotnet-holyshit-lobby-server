using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Utils.Decode;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Models;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Services.LoadBalancing;
using HolyShitServer.Src.Core.Client;

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

  public static async Task<GamePacketMessage> HandleChatMessageRequest(ClientSession client, uint sequence, C2SChatMessageRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();
    var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 채팅 메시지 전송 처리
    var result = await roomService.SendChatMessage(client.UserId, request.Message, request.MessageType);
    // 채팅 알림 전송
    if (result.Success)
    {
      // Redis에서 유저 정보 조회
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(client.UserId, dbContext);
      if (userCharacterData != null)
      {
        // Redis에서 모든 온라인 유저 ID 목록 가져오기
        var allOnlineUserIds = await redisService.GetAllOnlineUserIds();
        var uniqueUserIds = allOnlineUserIds.Distinct().Where(id => id > 0).ToList();
        if (uniqueUserIds.Any())
        { 
          // 채팅 알림 생성 및 브로드캐스트
          var notification = NotificationHelper.CreateChatMessageNotification(
            userCharacterData.Nickname,
            request.Message,
            request.MessageType,
            uniqueUserIds
          );

          var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
          await messageQueueService.BroadcastMessage(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            uniqueUserIds
          );
        }
      }
    }

    return ResponseHelper.CreateChatMessageResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleGamePrepareRequest(ClientSession client, uint sequence, C2SGamePrepareRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

    // 게임 준비 요청 처리
    var result = await roomService.GamePrepare(client.UserId);
    // 게임 준비 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetUserIds = RoomModel.Instance.GetRoomTargetUserIds(currentRoom.Id, 0); // 모든 유저
        if (targetUserIds.Any())
        {
          var notification = NotificationHelper.CreateGamePrepareNotification(
            currentRoom.ToProto(),
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

    return ResponseHelper.CreateGamePrepareResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleGameStartRequest(ClientSession client, uint sequence, C2SGameStartRequest request)
  {
    using var scope = client.ServiceProvider.CreateScope();
    var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();
    var jwtTokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
    var clientManager = scope.ServiceProvider.GetRequiredService<ClientManager>();

    // 게임 시작 처리
    var result = await roomService.GameStart(client.UserId);
    // 게임 시작 알림
    if (result.Success)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        var targetUserIds = RoomModel.Instance.GetRoomTargetUserIds(currentRoom.Id, 0); // 모든 유저
        if (targetUserIds.Any())
        {
          // 방에 있는 모든 유저 게임 시작 상태 설정
          foreach (var userId in targetUserIds)
          {
            if (clientManager.GetSessionByUserId(userId) is ClientSession userSession)
              userSession.SetGameStarted();
          }

          // 게임 서버 정보 가져오기
          var gameServer = result.Data;
          // 게임 서버 정보 생성
          var serverInfo = new ServerInfoData
          {
            Host = "127.0.0.1",//gameServer.Host,
            Port = 5000,//gameServer.Port,
            Token = jwtTokenService.GenerateGameServerToken()
          };

          var notification = NotificationHelper.CreateGameStartNotification(serverInfo, targetUserIds);
          var messageQueueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
          await messageQueueService.BroadcastMessage(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            targetUserIds
          );

          // 모든 처리가 끝난 후 방 삭제
          RoomModel.Instance.RemoveRoom(currentRoom.Id);
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