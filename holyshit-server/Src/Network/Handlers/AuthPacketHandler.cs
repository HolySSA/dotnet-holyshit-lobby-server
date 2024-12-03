using Google.Protobuf;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  public static async Task HandleRegisterRequest(TcpClientHandler client, uint sequence, C2SRegisterRequest request)
  {
    try
    {
      Console.WriteLine($"Login Request 원본: {request}"); // protobuf 객체 전체 출력
      Console.WriteLine($"Login Request 수신: Email='{request.Email}', Password='{request.Password}'");

      // TODO: 실제 회원가입 로직 구현

      // 1. 이메일 형식 검증
      // 2. 이메일 중복 확인
      // 3. 닉네임 중복 확인
      // 4. 비밀번호 규칙 검증
      // 5. DB에 저장

      // 임시 응답 (성공 케이스)
      var response = new S2CRegisterResponse
      {
          Success = true,
          Message = "회원가입이 완료되었습니다!",
          FailCode = GlobalFailCode.NoneFailcode
      };

      await client.SendResponseAsync(PacketId.S2CregisterResponse, sequence, response);
      Console.WriteLine($"Register Response 전송: Success={response.Success}, Message={response.Message}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Register Request 처리 중 오류: {ex.Message}");

      // 실패 응답
      var errorResponse = new S2CRegisterResponse
      {
          Success = false,
          Message = "회원가입 처리 중 오류가 발생했습니다",
          FailCode = GlobalFailCode.RegisterFailed
      };

      await client.SendResponseAsync(PacketId.S2CregisterResponse, sequence, errorResponse);
    }
  }

  public static async Task HandleLoginRequest(TcpClientHandler client, uint sequence, C2SLoginRequest request)
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