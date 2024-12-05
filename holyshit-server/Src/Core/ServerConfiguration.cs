using HolyShitServer.Src.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Core;

public static class ServerConfiguration
{
  public static IServiceProvider ConfigureServices()
  {
    // 설정 초기화
    var configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json")
      .Build();

    // 서비스 컬렉션 설정
    var services = new ServiceCollection();

    // 데이터베이스 서비스 등록
    services.AddDatabaseServices(configuration);

    return services.BuildServiceProvider();
  }

  private static async Task InitializeServerAsync(IServiceProvider serviceProvider)
  {
    // 데이터베이스 초기화
    await DatabaseConfig.InitializeDatabaseAsync(serviceProvider);

    // 게임 데이터 로드
    var gameDataManager = new GameDataManager();
    await gameDataManager.InitializeDataAsync();

    // 패킷매니저, 핸들러매니저 초기화
    //PacketManager.Initialize();
    //HandlerManager.Initialize();
  }
}