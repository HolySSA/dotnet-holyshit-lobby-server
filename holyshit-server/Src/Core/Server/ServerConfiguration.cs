using HolyShitServer.DB.Configuration;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Services.Extensions;
using HolyShitServer.Src.Services.LoadBalancing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Core;

public static class ServerConfiguration
{
  public static IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();
        
    var configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
    services.AddSingleton<IConfiguration>(configuration);

    services
      .AddInfrastructure(configuration)
      .AddGameServices()
      .AddLoadBalancing();

    return services.BuildServiceProvider();
  }

  public static async Task InitializeServicesAsync(IServiceProvider serviceProvider)
  {
    // 데이터베이스 초기화
    await DatabaseConfig.InitializeDatabaseAsync(serviceProvider);

    // 게임 데이터 로드
    var gameDataManager = serviceProvider.GetRequiredService<GameDataManager>();
    await gameDataManager.InitializeDataAsync();

    // 게임 서버 초기화
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var loadBalancer = serviceProvider.GetRequiredService<LoadBalancer>();
    var gameServers = configuration.GetSection("GameServers").Get<List<GameServerInfo>>();
    if (gameServers != null)
    {
      foreach (var server in gameServers)
      {
        await loadBalancer.RegisterGameServer(
          server.Host,
          server.Port,
          server.MaxPlayers
        );
      }
      Console.WriteLine($"{gameServers.Count}개의 게임 서버가 등록되었습니다.");
    }

    // 패킷매니저, 핸들러매니저 초기화
    PacketManager.Initialize();
    HandlerManager.Initialize(serviceProvider);
  }
}