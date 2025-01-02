using HolyShitServer.DB.Configuration;
using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network.Protocol;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Services.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HolyShitServer.Src.Core;

public static class ServerConfiguration
{
  public static IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();
        
    // Configuration 설정
    var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
    services.AddSingleton<IConfiguration>(configuration);

    // 서비스 등록
    services.AddSingleton<IConnectionMultiplexer>(sp =>ConnectionMultiplexer
      .Connect(configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is not configured")));
    
    services.AddDbContext<ApplicationDbContext>();
    services.AddSingleton<TokenValidationService>();
    services.AddSingleton<GameDataManager>();

    return services.BuildServiceProvider();
  }

  public static async Task InitializeServicesAsync(IServiceProvider serviceProvider)
  {
    // 데이터베이스 초기화
    await DatabaseConfig.InitializeDatabaseAsync(serviceProvider);

    // 게임 데이터 로드
    var gameDataManager = new GameDataManager();
    await gameDataManager.InitializeDataAsync();

    // 패킷매니저, 핸들러매니저 초기화
    PacketManager.Initialize();
    HandlerManager.Initialize(serviceProvider);
  }
}