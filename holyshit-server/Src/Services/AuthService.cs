using HolyShitServer.DB.Contexts;
using HolyShitServer.DB.Entities;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserModel _userModel;

    public AuthService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _userModel = UserModel.Instance;
    }

    public async Task<ServiceResult<UserData>> Register(string email, string password, string nickname)
    {
        try
        {
            // 중복 검사
            if (await _dbContext.Users.AnyAsync(u => u.Email == email))
                return ServiceResult<UserData>.Error(GlobalFailCode.AuthenticationFailed, "이미 사용 중인 이메일입니다.");

            if (await _dbContext.Users.AnyAsync(u => u.Nickname == nickname))
                return ServiceResult<UserData>.Error(GlobalFailCode.AuthenticationFailed, "이미 사용 중인 닉네임입니다.");

            // DB 저장
            var user = new User
            {
                Email = email,
                Nickname = nickname,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var userData = new UserData
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Character = CreateDefaultCharacter()
            };

            return ServiceResult<UserData>.Ok(userData, "회원가입이 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Register 실패: {ex.Message}");
            return ServiceResult<UserData>.Error(GlobalFailCode.UnknownError);
        }
    }

    public async Task<ServiceResult<LoginResult>> Login(string email, string password, ClientSession client)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return ServiceResult<LoginResult>.Error(GlobalFailCode.AuthenticationFailed);

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return ServiceResult<LoginResult>.Error(GlobalFailCode.AuthenticationFailed);

            var existingUser = UserModel.Instance.GetUser(user.Id);
            if (existingUser != null)
            {
                existingUser.Client.Dispose();
                UserModel.Instance.RemoveUser(user.Id);
            }

            var userData = new UserData
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Character = CreateDefaultCharacter()
            };

            string token = "temp_token_" + user.Id;

            if (!UserModel.Instance.AddUser(user.Id, token, userData, client))
                return ServiceResult<LoginResult>.Error(GlobalFailCode.UnknownError);

            var loginResult = new LoginResult(token, userData);
            return ServiceResult<LoginResult>.Ok(loginResult, "로그인 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Login 실패: {ex.Message}");
            return ServiceResult<LoginResult>.Error(GlobalFailCode.UnknownError);
        }
    }

    private CharacterData CreateDefaultCharacter()
    {
        return new CharacterData
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
    }
}