using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services.Results;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<UserData>> Register(string email, string password, string nickname);
    Task<ServiceResult<LoginResult>> Login(string email, string password, ClientSession client);
}