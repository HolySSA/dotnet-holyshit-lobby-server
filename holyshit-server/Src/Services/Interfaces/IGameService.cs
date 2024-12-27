using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Services.Results;

namespace HolyShitServer.Src.Services.Interfaces;

public interface IGameService
{
  Task<ServiceResult<List<CharacterPositionData>>> UpdatePosition(long userId, double x, double y);
}
