using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services.Interfaces;

public interface IRoomService
{
  Task<ServiceResult<List<RoomData>>> GetRoomList();
  Task<ServiceResult<RoomData>> CreateRoom(int userId, string name, int maxUserNum);
  Task<ServiceResult<RoomData>> JoinRoom(int userId, int roomId);
  Task<ServiceResult<RoomData>> JoinRandomRoom(int userId);
  Task<ServiceResult> LeaveRoom(int userId);
  Task<ServiceResult> GameReady(int userId, bool isReady);
  Task<ServiceResult> GamePrepare(int userId);
  Task<ServiceResult> GameStart(int userId);
}