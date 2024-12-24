using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Services.Results;

public class LoginResult
{
    public string Token { get; set; }
    public UserData UserData { get; set; }

    public LoginResult(string token, UserData userData)
    {
        Token = token;
        UserData = userData;
    }
} 