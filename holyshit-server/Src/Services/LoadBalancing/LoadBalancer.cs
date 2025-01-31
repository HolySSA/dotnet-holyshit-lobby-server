using StackExchange.Redis;

namespace HolyShitServer.Src.Services.LoadBalancing;

public class LoadBalancer
{
  private readonly IConnectionMultiplexer _redis;
  private readonly IServerSelectionStrategy _selectionStrategy;

  private const string GAME_SERVERS_KEY = "game_servers";

  public LoadBalancer(IConnectionMultiplexer redis, IServerSelectionStrategy strategy)
  {
    _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    _selectionStrategy = strategy;
  }

  public async Task<GameServerInfo?> GetServerForRoom(int playerCount)
  {
    var servers = await GetAllGameServers();
    return _selectionStrategy.SelectServer(servers, playerCount);
  }

  private async Task<List<GameServerInfo>> GetAllGameServers()
  {
    var db = _redis.GetDatabase();
    var serverKeys = await db.SetMembersAsync(GAME_SERVERS_KEY);
    var servers = new List<GameServerInfo>();

    foreach (var key in serverKeys)
    {
      var hashFields = await db.HashGetAllAsync(key.ToString());
      if (hashFields.Length > 0)
      {
        var server = ConvertToGameServerInfo(hashFields);
        if (server != null)
        {
          servers.Add(server);
        }
      }
    }

    return servers;
  }

  public async Task RegisterGameServer(string host, int port, int maxPlayers)
  {
    var db = _redis.GetDatabase();
    var key = $"gameserver:{host}:{port}";

    var hashFields = new HashEntry[]
    {
      new HashEntry("host", host),
      new HashEntry("port", port),
      new HashEntry("currentPlayers", 0),
      new HashEntry("maxPlayers", maxPlayers),
      new HashEntry("isAvailable", "true"),
      new HashEntry("lastHeartbeat", DateTime.UtcNow.ToString("O"))
    };

    // 서버 정보 저장 및 서버 목록에 추가
    await Task.WhenAll(
      db.HashSetAsync(key, hashFields),
      db.SetAddAsync(GAME_SERVERS_KEY, key)
    );
  }

  public async Task UpdateServerStatus(string host, int port, int currentPlayers)
  {
    var db = _redis.GetDatabase();
    var key = $"gameserver:{host}:{port}";

    var hashFields = new HashEntry[]
    {
      new HashEntry("currentPlayers", currentPlayers),
      new HashEntry("lastHeartbeat", DateTime.UtcNow.ToString("O"))
    };

    await db.HashSetAsync(key, hashFields);
  }

  public async Task RemoveGameServer(string host, int port)
  {
    var db = _redis.GetDatabase();
    var key = $"gameserver:{host}:{port}";

    // 서버 정보 삭제 및 서버 목록에서 제거
    await Task.WhenAll(
      db.KeyDeleteAsync(key),
      db.SetRemoveAsync(GAME_SERVERS_KEY, key)
    );
  }

  public async Task StartHealthCheck(TimeSpan checkInterval, TimeSpan timeout)
  {
    while (true)
    {
      try
      {
        var servers = await GetAllGameServers();
        foreach (var server in servers)
        {
          if (DateTime.UtcNow - server.LastHeartbeat > timeout)
          {
            // 서버가 응답이 없으면 제거
            await RemoveGameServer(server.Host, server.Port);
            Console.WriteLine($"게임 서버 제거됨: {server.Host}:{server.Port} (응답 없음)");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"헬스 체크 중 오류 발생: {ex.Message}");
      }

      await Task.Delay(checkInterval);
    }
  }

  private GameServerInfo ConvertToGameServerInfo(HashEntry[] hashFields)
  {
    var dict = hashFields.ToDictionary(h => h.Name.ToString(), h => h.Value.ToString());

    return new GameServerInfo
    {
      Host = dict["host"],
      Port = int.Parse(dict["port"]),
      CurrentPlayers = int.Parse(dict["currentPlayers"]),
      MaxPlayers = int.Parse(dict["maxPlayers"]),
      IsAvailable = dict["isAvailable"].ToLower() == "true",
      LastHeartbeat = DateTime.Parse(dict["lastHeartbeat"])
    };
  }
}