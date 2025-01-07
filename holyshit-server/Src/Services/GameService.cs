using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services;

public class GameService : IGameService
{
  private readonly UserModel _userModel;
  private readonly RoomModel _roomModel;

  public GameService()
  {
    _userModel = UserModel.Instance;
    _roomModel = RoomModel.Instance;
  }

  public async Task<ServiceResult<List<CharacterPositionData>>> UpdatePosition(int userId, double x, double y)
  {
    try
    {
      return await Task.Run(() =>
      {
        // 유저 검증
        var userInfo = _userModel.GetUser(userId);
        if (userInfo == null)
          return ServiceResult<List<CharacterPositionData>>.Error(GlobalFailCode.AuthenticationFailed);

        // 방 검증
        var currentRoom = _roomModel.GetUserRoom(userId);
        if (currentRoom == null)
          return ServiceResult<List<CharacterPositionData>>.Error(GlobalFailCode.RoomNotFound);

        // 게임 중인지 확인
        if (currentRoom.State != RoomStateType.Ingame)
          return ServiceResult<List<CharacterPositionData>>.Error(GlobalFailCode.InvalidRoomState);

        // 위치 업데이트
        if (!currentRoom.UpdatePosition(userId, x, y))
          return ServiceResult<List<CharacterPositionData>>.Error(GlobalFailCode.UnknownError);

        // 업데이트된 모든 위치 정보 반환
        return ServiceResult<List<CharacterPositionData>>.Ok(currentRoom.GetAllPositions());
      });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[GameService] UpdatePosition 실패: {ex.Message}");
      return ServiceResult<List<CharacterPositionData>>.Error(GlobalFailCode.UnknownError);
    }
  }
}