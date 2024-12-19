using HolyShitServer.Src.Network.Packets;
using Holyshit.DB.Contexts;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.Src.Utils.FluentValidation;
using Holyshit.DB.Entities;
using HolyShitServer.Src.Utils.Decode;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Socket;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
  public static async Task<GamePacketMessage> HandleRegisterRequest(ClientSession client, uint sequence, C2SRegisterRequest request)
  {
    try
    {
      var validator = new RegisterRequestValidator();
      var validationResult = await validator.ValidateAsync(request);
      // 유효성 검사
      if (!validationResult.IsValid)
      {
        var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
        return ResponseHelper.CreateRegisterResponse(sequence, false, errors, GlobalFailCode.InvalidRequest);
      }

      using var scope = client.ServiceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 중복 검사
      if (await dbContext.Users.AnyAsync(u => u.Email == request.Email))
      {
        return ResponseHelper.CreateRegisterResponse(sequence, false, "이미 사용 중인 이메일입니다.", GlobalFailCode.AuthenticationFailed);
      }

      if (await dbContext.Users.AnyAsync(u => u.Nickname == request.Nickname))
      {
        return ResponseHelper.CreateRegisterResponse(sequence, false, "이미 사용 중인 닉네임입니다.", GlobalFailCode.AuthenticationFailed);
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
      return ResponseHelper.CreateRegisterResponse(sequence, true, "회원가입이 완료되었습니다!", GlobalFailCode.NoneFailcode);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Register Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateRegisterResponse(sequence, false, "회원가입 처리 중 오류가 발생했습니다", GlobalFailCode.UnknownError);
    }
  }

  public static async Task<GamePacketMessage> HandleLoginRequest(ClientSession client, uint sequence, C2SLoginRequest request)
  {
    try
    {
      using var scope = client.ServiceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
      if (user == null)
      {
        return ResponseHelper.CreateLoginResponse(
          sequence,
          false,
          "",
          failCode: GlobalFailCode.AuthenticationFailed
        );
      }

      if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
      {
        return ResponseHelper.CreateLoginResponse(
          sequence,
          false,
          "",
          failCode: GlobalFailCode.AuthenticationFailed
        );
      }

      var existingUser = UserModel.Instance.GetUser(user.Id);
      if (existingUser != null)
      {
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
        return ResponseHelper.CreateLoginResponse(
          sequence,
          false,
          "",
          failCode: GlobalFailCode.UnknownError
        );
      }

      Console.WriteLine($"[Auth] Login 응답 전송 완료: Email='{request.Email}', Id={user.Id}");
      return ResponseHelper.CreateLoginResponse(
        sequence,
        true,
        "로그인 완료!",
        token,
        userData,
        GlobalFailCode.NoneFailcode
      );
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Auth] Login Request 처리 중 오류: {ex.Message}");
      return ResponseHelper.CreateLoginResponse(
        sequence,
        false,
        "",
        failCode: GlobalFailCode.UnknownError
      );
    }
  }
}