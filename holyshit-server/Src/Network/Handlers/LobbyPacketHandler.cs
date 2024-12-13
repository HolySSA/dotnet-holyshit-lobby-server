using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Utils.Decode;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services;

namespace HolyShitServer.Src.Network.Handlers;

public static class LobbyPacketHandler
{
  private static readonly IRoomService _roomService = new RoomService();

  public static async Task<GamePacketMessage> HandleGetRoomListRequest(ClientSession client, uint sequence, C2SGetRoomListRequest request)
  {
    var result = await _roomService.GetRoomList(client);
    return ResponseHelper.CreateGetRoomListResponse(
      sequence,
      result.Success ? result.Data : null
    );
  }

  public static async Task<GamePacketMessage> HandleCreateRoomRequest(ClientSession client, uint sequence, C2SCreateRoomRequest request)
  {
    var result = await _roomService.CreateRoom(client, request.Name, request.MaxUserNum);
    return ResponseHelper.CreateCreateRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleJoinRoomRequest(ClientSession client, uint sequence, C2SJoinRoomRequest request)
  {
    var result = await _roomService.JoinRoom(client, request.RoomId);
    return ResponseHelper.CreateJoinRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleJoinRandomRoomRequest(ClientSession client, uint sequence, C2SJoinRandomRoomRequest request)
  {
    var result = await _roomService.JoinRandomRoom(client);
    return ResponseHelper.CreateJoinRandomRoomResponse(
      sequence,
      result.Success,
      result.Data,
      result.FailCode
    );
  }

  public static async Task<GamePacketMessage> HandleLeaveRoomRequest(ClientSession client, uint sequence, C2SLeaveRoomRequest request)
  {
    var result = await _roomService.LeaveRoom(client);
    return ResponseHelper.CreateLeaveRoomResponse(
      sequence,
      result.Success,
      result.FailCode
    );
  }
}