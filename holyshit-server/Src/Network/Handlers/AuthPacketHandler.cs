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
    HandlerManager.RegisterHandler<C2SRegisterRequest>(PacketId.C2SregisterRequest, HandleRegisterRequest);
    HandlerManager.RegisterHandler<C2SLoginRequest>(PacketId.C2SloginRequest, HandleLoginRequest);
  
    // 등록 후 핸들러 출력
    handlers = HandlerManager.GetRegisteredHandlers();
    Console.WriteLine($"등록 후 핸들러: {string.Join(", ", handlers)}");
  }

  public void UnregisterHandlers()
  {
    HandlerManager.UnregisterHandler(PacketId.C2SregisterRequest);
    HandlerManager.UnregisterHandler(PacketId.C2SloginRequest);
  }

  private async Task HandleRegisterRequest(uint sequence, C2SRegisterRequest request)
  {
    try 
    {
      Console.WriteLine($"Login Request 원본: {request}"); // protobuf 객체 전체 출력
      Console.WriteLine($"Login Request 수신: Email='{request.Email}', Password='{request.Password}'");
      
      // TODO: 실제 회원가입 로직 구현
      await Task.CompletedTask; // 임시로 비동기 작업 완료 표시
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Register Request 처리 중 오류: {ex.Message}");
    }
  }

  private async Task HandleLoginRequest(uint sequence, C2SLoginRequest request)
  {
    try 
    {
      Console.WriteLine($"Login Request 원본: {request}"); // protobuf 객체 전체 출력
      Console.WriteLine($"Login Request 수신: Email='{request.Email}', Password='{request.Password}'");
        
      // TODO: 실제 로그인 로직 구현
      await Task.CompletedTask; // 임시로 비동기 작업 완료 표시
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Login Request 처리 중 오류: {ex.Message}");
    }
  }
}