using HolyShitServer.Src.Network.Packets;
using Holyshit.DB.Contexts;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.Src.Utils.FluentValidation;
using Holyshit.DB.Entities;
using HolyShitServer.Src.Utils.Decode;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using HolyShitServer.Src.Models;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  public static async Task HandleRegisterRequest(TcpClientHandler client, uint sequence, C2SRegisterRequest request)
  {
    try
    {
      var validator = new RegisterRequestValidator();
      var validationResult = await validator.ValidateAsync(request);

      if (!validationResult.IsValid)
      {
        var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
        Console.WriteLine($"[Auth] Register 유효성 검사 실패: {errors}");
        await ResponseHelper.SendRegisterResponse(client, sequence, false, errors, GlobalFailCode.InvalidRequest);
        return;
      }

      using var scope = client.ServiceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 중복 검사
      if (await dbContext.Users.AnyAsync(u => u.Email == request.Email))
      {
        Console.WriteLine($"[Auth] Register 이메일 중복: {request.Email}");
        await ResponseHelper.SendRegisterResponse(client, sequence, false, "이미 사용 중인 이메일입니다.", GlobalFailCode.AuthenticationFailed);
        return;
      }

      if (await dbContext.Users.AnyAsync(u => u.Nickname == request.Nickname))
      {
        Console.WriteLine($"[Auth] Register 닉네임 중복: {request.Nickname}");
        await ResponseHelper.SendRegisterResponse(client, sequence, false, "이미 사용 중인 닉네임입니다.", GlobalFailCode.AuthenticationFailed);
        return;
      }

      // DB 저장
      var user = new User
      {
        Email = request.Email,
        Nickname = request.Nickname,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Users.Add(user);
      await dbContext.SaveChangesAsync();

      Console.WriteLine($"[Auth] Register 성공: Email='{user.Email}', Nickname='{user.Nickname}', Id={user.Id}");
      await ResponseHelper.SendRegisterResponse(client, sequence, true, "회원가입이 완료되었습니다!", GlobalFailCode.NoneFailcode);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Register Request 처리 중 오류: {ex.Message}");
      await ResponseHelper.SendRegisterResponse(client, sequence, false, "회원가입 처리 중 오류가 발생했습니다", GlobalFailCode.UnknownError);
    }
  }

  public static async Task HandleLoginRequest(TcpClientHandler client, uint sequence, C2SLoginRequest request)
  {
    try
    {
      using var scope = client.ServiceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
      if (user == null)
      {
        Console.WriteLine($"[Auth] Login 실패: 사용자 없음 - Email='{request.Email}'");

        await ResponseHelper.SendLoginResponse(
          client,
          sequence,
          false,
          "이메일 또는 비밀번호가 일치하지 않습니다.",
          failCode: GlobalFailCode.AuthenticationFailed
        );

        return;
      }

      if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
      {
        Console.WriteLine($"[Auth] Login 실패: 비밀번호 불일치 - Email='{request.Email}'");

        await ResponseHelper.SendLoginResponse(
          client,
          sequence,
          false,
          "이메일 또는 비밀번호가 일치하지 않습니다.",
          failCode: GlobalFailCode.AuthenticationFailed
        );

        return;
      }

      var existingUser = UserModel.Instance.GetUser(user.Id);
      if (existingUser != null)
      {
        Console.WriteLine($"[Auth] Login 실패: 이미 로그인된 사용자 - Email='{request.Email}', Id={user.Id}");
        // 기존 연결 종료
        existingUser.Client.Dispose();
        UserModel.Instance.RemoveUser(user.Id);
      }

      // 기본 캐릭터 데이터 생성
      var characterData = new CharacterData
      {
        CharacterType = CharacterType.NoneCharacter,
        RoleType = RoleType.NoneRole,
        Hp = 0,
        Weapon = 0,
        StateInfo = new CharacterStateInfoData
        {
          State = CharacterStateType.NoneCharacterState,
          NextState = CharacterStateType.NoneCharacterState,
          NextStateAt = 0,
          StateTargetUserId = 0
        },
        BbangCount = 0,
        HandCardsCount = 0
      };

      var userData = new UserData
      {
        Id = user.Id,
        Nickname = user.Nickname,
        Character = characterData
      };

      // 토큰 구현 필요
      string token = "temp_token_" + user.Id;

      // UserModel에 유저 추가
      if (!UserModel.Instance.AddUser(user.Id, token, userData, client))
      {
        Console.WriteLine($"[Auth] Login 실패: UserModel 추가 실패 - Email='{request.Email}', Id={user.Id}");
        await ResponseHelper.SendLoginResponse(
          client,
          sequence,
          false,
          "로그인 처리 중 오류가 발생했습니다",
          failCode: GlobalFailCode.UnknownError
        );
        
        return;
      }

      await ResponseHelper.SendLoginResponse(
        client,
        sequence,
        true,
        "로그인이 완료되었습니다!",
        token,
        userData,
        GlobalFailCode.NoneFailcode
      );

      Console.WriteLine($"[Auth] Login 응답 전송 완료: Email='{request.Email}', Id={user.Id}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Auth] Login Request 처리 중 오류: {ex.Message}");
      await ResponseHelper.SendLoginResponse(
        client,
        sequence,
        false,
        "로그인 처리 중 오류가 발생했습니다",
        failCode: GlobalFailCode.UnknownError
      );
    }
  }
}