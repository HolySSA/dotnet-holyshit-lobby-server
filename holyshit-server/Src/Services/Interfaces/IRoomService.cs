using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services.Interfaces;

public interface IRoomService
{
  Task<ServiceResult<RoomData>> CreateRoom(ClientSession client, string name, int maxUserNum);
  Task<ServiceResult<RoomData>> JoinRoom(ClientSession client, int roomId);
  Task<ServiceResult<RoomData>> JoinRandomRoom(ClientSession client);
  Task<ServiceResult> LeaveRoom(ClientSession client);
  Task<ServiceResult<List<RoomData>>> GetRoomList(ClientSession client);
  Task<ServiceResult> GameReady(ClientSession client, bool isReady);
  Task<ServiceResult> GamePrepare(ClientSession client);
  Task<ServiceResult> GameStart(ClientSession client);
}