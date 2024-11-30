using Google.Protobuf;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Handlers;

public class AuthPacketHandler : IPacketHandler
{
  private readonly TcpClientHandler _clientHandler;

  public AuthPacketHandler(TcpClientHandler clientHandler)
  {
    _clientHandler = clientHandler;
  }

  public void RegisterHandlers()
  {
    MessageHandlerManager.RegisterHandler<C2SRegisterRequest>(PacketId.C2SRegisterRequest, HandleRegisterRequest);
    MessageHandlerManager.RegisterHandler<C2SLoginRequest>(PacketId.C2SLoginRequest, HandleLoginRequest);
  }

  public void UnregisterHandlers()
  {
    MessageHandlerManager.UnregisterHandler(PacketId.C2SRegisterRequest);
    MessageHandlerManager.UnregisterHandler(PacketId.C2SLoginRequest);
  }

  private async Task HandleRegisterRequest(C2SRegisterRequest request)
  {
    Console.WriteLine($"Register Request: {request.Username}");
  }

  private async Task HandleLoginRequest(C2SLoginRequest request)
  {
    Console.WriteLine($"Login Request: {request.Username}");
  }
}