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
            var roomList = await Task.FromResult(_roomModel.GetRoomList());
            return ServiceResult<List<RoomData>>.Ok(roomList);
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
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[RoomService] CreateRoom 실패: {ex.Message}");
        return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
      }
    }

    public async Task<ServiceResult<RoomData>> JoinRoom(ClientSession client, int roomId)
    {
      // 1. 유저 정보 확인
      var userInfo = _userModel.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      // 2. 이미 방에 있는지 확인
      var existingRoom = _roomModel.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 3. 방 입장 처리
      if (!_roomModel.JoinRoom(roomId, userInfo.UserData))
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      var updatedRoom = _roomModel.GetRoom(roomId);
      if (updatedRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      // 4. 다른 유저들에게 알림
      await NotifyRoomMembers(client, updatedRoom, userInfo);

      return ServiceResult<RoomData>.Ok(updatedRoom);
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