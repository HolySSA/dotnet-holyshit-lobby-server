namespace HolyShitServer.Src.Services.LoadBalancing;

public interface IServerSelectionStrategy
{
  GameServerInfo SelectServer(IEnumerable<GameServerInfo> servers, int requiredSlots);
}

/// <summary>
/// 라운드 로빈 전략
/// </summary>
public class RoundRobinStrategy : IServerSelectionStrategy
{
  private int currentIndex = 0;
  
  public GameServerInfo SelectServer(IEnumerable<GameServerInfo> servers, int requiredSlots)
  {
    var availableServers = servers.Where(s => s.IsAvailable && (s.MaxPlayers - s.CurrentPlayers) >= requiredSlots).ToList();

    if (!availableServers.Any())
      return null;

    currentIndex = (currentIndex + 1) % availableServers.Count;
    return availableServers[currentIndex];
  }
}