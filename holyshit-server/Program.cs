using HolyShitServer.Src.Core;
using HolyShitServer.Src.Core.Server;

namespace HolyShitServer;

class Program
{
  private static ServerManager? _serverManager;

  public static async Task Main()
  {
    // 서버 종료 시 실행되는 이벤트 핸들러
    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

    try
    {
      // 서비스 설정
      var serviceProvider = ServerConfiguration.ConfigureServices();
      // 서비스 초기화
      await ServerConfiguration.InitializeServicesAsync(serviceProvider);
      // 서버 시작
      _serverManager = new ServerManager(serviceProvider);
      await _serverManager.StartAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"서버 실행 중 오류 발생: {ex.Message}");
    }
  }

  private static void OnProcessExit(object? sender, EventArgs e)
  {
    _serverManager?.StopAsync().Wait();
  }
}
