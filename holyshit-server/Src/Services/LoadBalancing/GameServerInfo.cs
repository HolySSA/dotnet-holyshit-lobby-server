namespace HolyShitServer.Src.Services.LoadBalancing;

public class GameServerInfo
{
  public string Host { get; set; } = string.Empty;
  public int Port { get; set; }
  public int CurrentPlayers { get; set; }
  public int MaxPlayers { get; set; }
  public bool IsAvailable { get; set; }
  public DateTime LastHeartbeat { get; set; }

  public string GetRedisKey() => $"gameserver:{Host}:{Port}";
}