using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Utils.Decode;

namespace HolyShitServer.Src.Network.Handlers;

public static class LobbyPacketHandler
{
  public static async Task<GamePacketMessage> HandleGetRoomListRequest(ClientSession client, uint sequence, C2SGetRoomListRequest request)
  {
    try
    {
      var roomList = await Task.FromResult(RoomModel.Instance.GetRoomList());
        
      return ResponseHelper.CreateGetRoomListResponse(
        sequence,
        roomList
      );
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] GetRoomList Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateGetRoomListResponse(
        sequence,
        null
      );
    }
  }

  public static async Task<GamePacketMessage> HandleCreateRoomRequest(ClientSession client, uint sequence, C2SCreateRoomRequest request)
  {
    try
    {
      var userInfo = await Task.Run(() => UserModel.Instance.GetAllUsers().FirstOrDefault(u => u.Client == client));
      if (userInfo == null)
      {
        Console.WriteLine("[Lobby] CreateRoom 실패: 인증되지 않은 사용자");
        return ResponseHelper.CreateCreateRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.AuthenticationFailed
        );
      }

      // 2. 이미 방에 있는지 확인
      var existingRoom = await Task.Run(() => RoomModel.Instance.GetUserRoom(userInfo.UserId));
      if (existingRoom != null)
      {
        Console.WriteLine($"[Lobby] CreateRoom 실패: 이미 방에 있는 사용자 - UserId={userInfo.UserId}, RoomId={existingRoom.Id}");
        return ResponseHelper.CreateCreateRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.CreateRoomFailed
        );
      }

      // 3. 방 생성 요청 유효성 검사
      if (string.IsNullOrEmpty(request.Name) || request.MaxUserNum < 2 || request.MaxUserNum > 8)
      {
        Console.WriteLine($"[Lobby] CreateRoom 실패: 잘못된 요청 - Name='{request.Name}', MaxUserNum={request.MaxUserNum}");
        return ResponseHelper.CreateCreateRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.InvalidRequest
        );
      }

      // 4. 방 생성
      var room = RoomModel.Instance.CreateRoom(
        request.Name,
        request.MaxUserNum,
        userInfo.UserId,
        userInfo.UserData
      );

      if (room != null)
      {
        Console.WriteLine($"[Lobby] CreateRoom 성공: RoomId={room.Id}, Name='{room.Name}', Owner={room.OwnerId}");
        return ResponseHelper.CreateCreateRoomResponse(
          sequence,
          true,
          room,
          GlobalFailCode.NoneFailcode);
      }
      else
      {
        Console.WriteLine("[Lobby] CreateRoom 실패: 방 생성 실패");
        return ResponseHelper.CreateCreateRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.CreateRoomFailed);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] CreateRoom Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateCreateRoomResponse(
        sequence,
        false,
        null,
        GlobalFailCode.UnknownError);
    }
  }

  public static async Task<GamePacketMessage> HandleJoinRoomRequest(ClientSession client, uint sequence, C2SJoinRoomRequest request)
  {
    try
    {
      // 1. 유저 정보 확인
      var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
      {
        Console.WriteLine("[Lobby] JoinRoom 실패: 인증되지 않은 사용자");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.AuthenticationFailed
        );
      }

      // 2. 이미 방에 있는지 확인
      var existingRoom = RoomModel.Instance.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
      {
        Console.WriteLine($"[Lobby] JoinRoom 실패: 이미 방에 있는 사용자 - UserId={userInfo.UserId}, RoomId={existingRoom.Id}");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.JoinRoomFailed
        );
      }

      // 3. 요청한 방이 존재하는지 확인
      var targetRoom = RoomModel.Instance.GetRoom(request.RoomId);
      if (targetRoom == null)
      {
        Console.WriteLine($"[Lobby] JoinRoom 실패: 존재하지 않는 방 - RoomId={request.RoomId}");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.RoomNotFound
        );
      }

      // 4. 인원 수 체크
      if (targetRoom.Users.Count >= targetRoom.MaxUserNum)
      {
        Console.WriteLine($"[Lobby] JoinRoom 실패: 방이 가득 참 - RoomId={request.RoomId}");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.JoinRoomFailed
        );
      }
      // 5. 상태 체크
      if (targetRoom.State != RoomStateType.Wait)
      {
        Console.WriteLine($"[Lobby] JoinRoom 실패: 입장할 수 없는 방 상태 - RoomId={request.RoomId}, State={targetRoom.State}");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.InvalidRoomState
        );
      }

      // 6. 방 입장 처리
      if (RoomModel.Instance.JoinRoom(request.RoomId, userInfo.UserData))
      {
        Console.WriteLine($"[Lobby] JoinRoom 성공: UserId={userInfo.UserId}, RoomId={request.RoomId}");

        // 7. 방에 있는 다른 유저들에게 새 유저 입장 알림
        var updatedRoom = RoomModel.Instance.GetRoom(request.RoomId);
        if (updatedRoom != null)
        {
          // 방의 모든 유저의 세션 ID를 가져옴 (입장한 유저 제외)
          var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(updatedRoom, userInfo.UserId);
          if (targetSessionIds.Any())
          {
            // 입장 알림 생성 및 전송
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

        // 7. 입장한 유저에게 방 정보 전송
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          true,
          updatedRoom,
          GlobalFailCode.NoneFailcode
        );
      }
      else
      {
        Console.WriteLine($"[Lobby] JoinRoom 실패: 방 입장 실패 - RoomId={request.RoomId}");
        return ResponseHelper.CreateJoinRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.JoinRoomFailed
        );
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] JoinRoom Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateJoinRoomResponse(
        sequence,
        false,
        null,
        GlobalFailCode.UnknownError
      );
    }
  }

  public static async Task<GamePacketMessage> HandleJoinRandomRoomRequest(ClientSession client, uint sequence, C2SJoinRandomRoomRequest request)
  {
    try
    {
      // 1. 유저 정보 확인
      var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
      {
        Console.WriteLine("[Lobby] JoinRandomRoom 실패: 인증되지 않은 사용자");
        return ResponseHelper.CreateJoinRandomRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.AuthenticationFailed
        );
      }

      // 2. 이미 방에 있는지 확인
      var existingRoom = RoomModel.Instance.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
      {
        Console.WriteLine($"[Lobby] JoinRandomRoom 실패: 이미 방에 있는 사용자 - UserId={userInfo.UserId}");
        return ResponseHelper.CreateJoinRandomRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.JoinRoomFailed
        );
      }

      // 3. 입장 가능한 방 목록 필터링
      var availableRooms = RoomModel.Instance.GetRoomList()
        .Where(r => r.Users.Count < r.MaxUserNum && r.State == RoomStateType.Wait)
        .ToList();

      if (!availableRooms.Any())
      {
        Console.WriteLine("[Lobby] JoinRandomRoom 실패: 입장 가능한 방이 없음");
        return ResponseHelper.CreateJoinRandomRoomResponse(
          sequence,
          false,
          null,
          GlobalFailCode.RoomNotFound
        );
      }

      // 4. 랜덤하게 방 선택
      var random = new Random();
      var selectedRoom = availableRooms[random.Next(availableRooms.Count)];

      // 5. 선택된 방 입장
      if (RoomModel.Instance.JoinRoom(selectedRoom.Id, userInfo.UserData))
      {
        Console.WriteLine($"[Lobby] JoinRandomRoom 성공: UserId={userInfo.UserId}, RoomId={selectedRoom.Id}");

        // 6. 방에 있는 다른 유저들에게 새 유저 입장 알림
        var updatedRoom = RoomModel.Instance.GetRoom(selectedRoom.Id);
        if (updatedRoom != null)
        {
          var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(updatedRoom, userInfo.UserId);
          if (targetSessionIds.Any())
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

        return ResponseHelper.CreateJoinRandomRoomResponse(
          sequence,
          true,
          updatedRoom,
          GlobalFailCode.NoneFailcode
        );
      }

      Console.WriteLine($"[Lobby] JoinRandomRoom 실패: 방 입장 실패 - RoomId={selectedRoom.Id}");
      return ResponseHelper.CreateJoinRandomRoomResponse(
        sequence,
        false,
        null,
        GlobalFailCode.JoinRoomFailed
      );
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] JoinRandomRoom Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateJoinRandomRoomResponse(
        sequence,
        false,
        null,
        GlobalFailCode.UnknownError
      );
    }
  }

  public static async Task<GamePacketMessage> HandleLeaveRoomRequest(ClientSession client, uint sequence, C2SLeaveRoomRequest request)
  {
    try
    {
      // 1. 유저 정보 확인
      var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
      {
        Console.WriteLine("[Lobby] LeaveRoom 실패: 인증되지 않은 사용자");
        return ResponseHelper.CreateLeaveRoomResponse(
          sequence,
          false,
          GlobalFailCode.AuthenticationFailed
        );
      }

      // 2. 현재 있는 방 확인
      var currentRoom = RoomModel.Instance.GetUserRoom(userInfo.UserId);
      if (currentRoom == null)
      {
        Console.WriteLine($"[Lobby] LeaveRoom 실패: 방에 없는 사용자 - UserId={userInfo.UserId}");
        return ResponseHelper.CreateLeaveRoomResponse(
          sequence,
          false,
          GlobalFailCode.RoomNotFound
        );
      }

      // 3. 방에 있는 다른 유저들에게 알림을 보내기 위해 세션 ID 목록 미리 가져오기
      var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(currentRoom, userInfo.UserId);

      // 4. 방 나가기 처리
      if (RoomModel.Instance.LeaveRoom(userInfo.UserId))
      {
        Console.WriteLine($"[Lobby] LeaveRoom 성공: UserId={userInfo.UserId}, RoomId={currentRoom.Id}");

        // 5. 방에 있는 다른 유저들에게 나간 유저 알림
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

        return ResponseHelper.CreateLeaveRoomResponse(
          sequence,
          true,
          GlobalFailCode.NoneFailcode
        );
      }

      Console.WriteLine($"[Lobby] LeaveRoom 실패: 방 나가기 실패 - UserId={userInfo.UserId}, RoomId={currentRoom.Id}");
      return ResponseHelper.CreateLeaveRoomResponse(
        sequence,
        false,
        GlobalFailCode.LeaveRoomFailed
      );
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] LeaveRoom Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateLeaveRoomResponse(
        sequence,
        false,
        GlobalFailCode.UnknownError
      );
    }
  }
}