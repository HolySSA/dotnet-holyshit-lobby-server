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
    Console.WriteLine("AuthPacketHandler - 핸들러 등록 시작");

    // 현재 등록된 모든 핸들러 출력
    var handlers = HandlerManager.GetRegisteredHandlers();
    Console.WriteLine($"현재 등록된 핸들러: {string.Join(", ", handlers)}");

    HandlerManager.RegisterHandler<C2SRegisterRequest>(PacketId.C2SregisterRequest, HandleRegisterRequest);
    HandlerManager.RegisterHandler<C2SLoginRequest>(PacketId.C2SloginRequest, HandleLoginRequest);
  
    // 등록 후 핸들러 출력
    handlers = HandlerManager.GetRegisteredHandlers();
    Console.WriteLine($"등록 후 핸들러: {string.Join(", ", handlers)}");
    
    // 등록된 값 확인
    Console.WriteLine($"Register handler ID: {PacketId.C2SregisterRequest} ({(int)PacketId.C2SregisterRequest})");
    Console.WriteLine($"Login handler ID: {PacketId.C2SloginRequest} ({(int)PacketId.C2SloginRequest})");
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
      Console.WriteLine($"Register Request 수신: Email={request.Email}, Password={request.Password}, Nickname={request.Nickname}");
        
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
      Console.WriteLine($"Login Request 수신: Email={request.Email}, Password={request.Password}");
        
      // TODO: 실제 로그인 로직 구현
      await Task.CompletedTask; // 임시로 비동기 작업 완료 표시
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Login Request 처리 중 오류: {ex.Message}");
    }
  }
}