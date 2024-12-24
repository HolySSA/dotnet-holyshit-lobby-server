using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Utils.FluentValidation;
using HolyShitServer.Src.Utils.Decode;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services;
using Microsoft.Extensions.DependencyInjection;
using HolyShitServer.DB.Contexts;

namespace HolyShitServer.Src.Network.Handlers;

public static class AuthPacketHandler
{
    public static async Task<GamePacketMessage> HandleRegisterRequest(ClientSession client, uint sequence, C2SRegisterRequest request)
    {
        if (client.ServiceScope?.ServiceProvider == null)
        {
            Console.WriteLine("[AuthHandler] ServiceScope가 설정되지 않았습니다.");
            return ResponseHelper.CreateRegisterResponse(
                sequence,
                false,
                "서버 오류가 발생했습니다.",
                GlobalFailCode.UnknownError
            );
        }

        var authService = client.ServiceScope.ServiceProvider.GetRequiredService<IAuthService>();

        var validator = new RegisterRequestValidator();
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ResponseHelper.CreateRegisterResponse(sequence, false, errors, GlobalFailCode.InvalidRequest);
        }

        var result = await authService.Register(request.Email, request.Password, request.Nickname);
        return ResponseHelper.CreateRegisterResponse(
            sequence,
            result.Success,
            result.Message,
            result.FailCode
        );
    }

    public static async Task<GamePacketMessage> HandleLoginRequest(ClientSession client, uint sequence, C2SLoginRequest request)
    {
        if (client.ServiceScope?.ServiceProvider == null)
        {
            Console.WriteLine("[AuthHandler] ServiceScope가 설정되지 않았습니다.");
            return ResponseHelper.CreateLoginResponse(
                sequence,
                false,
                "서버 오류가 발생했습니다.",
                null,
                null,
                GlobalFailCode.UnknownError
            );
        }

        var authService = client.ServiceScope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await authService.Login(request.Email, request.Password, client);

        return ResponseHelper.CreateLoginResponse(
            sequence,
            result.Success,
            result.Message,
            result.Success && result.Data != null ? result.Data.Token : null,
            result.Success && result.Data != null ? result.Data.UserData : null,
            result.FailCode
        );
    }
}