using System.Net;
using System.Net.Sockets;
using HolyShitServer.Src.Constants;
using HolyShitServer.Src.Core.Client;

namespace HolyShitServer.Src.Core.Server;

public class ServerManager
{
  private readonly TcpListener _tcpListener; // TCP 서버 객체
  private readonly CancellationTokenSource _serverCts; // 서버 종료 토큰 (서버 관리)
  private readonly IServiceProvider _serviceProvider; // 서비스 프로바이더 객체

  public ServerManager(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
    _tcpListener = new TcpListener(IPAddress.Parse(ServerConstants.HOST), ServerConstants.PORT);
    _serverCts = new CancellationTokenSource();
    
    ClientManager.Initialize(serviceProvider);
  }

  public async Task StartAsync()
  {
    try
    {
      _tcpListener.Start();
      Console.WriteLine($"서버 - {ServerConstants.HOST}:{ServerConstants.PORT}");

      while (!_serverCts.Token.IsCancellationRequested)
      {
        try
        {
            var client = await _tcpListener.AcceptTcpClientAsync(_serverCts.Token);
            _ = ClientManager.HandleClientAsync(client);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클라이언트 연결 처리 중 오류: {ex.Message}");
        }
      }
    }
    finally
    {
      await StopAsync();
    }
  }

  public async Task StopAsync()
  {
    _serverCts.Cancel();
    _tcpListener.Stop();
    await ClientManager.CleanupClientsAsync();
    _serverCts.Dispose();
  }
}