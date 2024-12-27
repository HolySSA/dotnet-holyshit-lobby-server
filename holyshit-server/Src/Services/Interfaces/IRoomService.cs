using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services.Interfaces;

public interface IRoomService
{
  Task<ServiceResult<List<RoomData>>> GetRoomList(long userId);
  Task<ServiceResult<RoomData>> CreateRoom(long userId, string name, int maxUserNum);
  Task<ServiceResult<RoomData>> JoinRoom(long userId, int roomId);
  Task<ServiceResult<RoomData>> JoinRandomRoom(long userId);
  Task<ServiceResult> LeaveRoom(long userId);
  Task<ServiceResult> GameReady(long userId, bool isReady);
  Task<ServiceResult> GamePrepare(long userId);
  Task<ServiceResult> GameStart(long userId);
}