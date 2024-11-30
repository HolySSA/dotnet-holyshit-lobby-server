namespace HolyShitServer.Src.Network.Handlers;

public interface IPacketHandler
{
  void RegisterHandlers();
  void UnregisterHandlers();
}