using System.Net;
using System.Net.Sockets;
using HolyShitServer.Src.Constants;
using HolyShitServer.Src.Core.Client;
using HolyShitServer.Src.Network.Socket;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Core.Server;

public class ServerManager
{
  private readonly TcpListener _tcpListener; // TCP 서버 객체
  private readonly CancellationTokenSource _serverCts; // 서버 종료 토큰 (서버 관리)
  private readonly IServiceProvider _serviceProvider; // 서비스 프로바이더 객체
  private readonly ClientManager _clientManager;

  public ServerManager(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
    _tcpListener = new TcpListener(IPAddress.Parse(ServerConstants.HOST), ServerConstants.PORT);
    _serverCts = new CancellationTokenSource();
    _clientManager = serviceProvider.GetRequiredService<ClientManager>();
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
          _ = HandleClientAsync(client);
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

  private async Task HandleClientAsync(TcpClient client)
  {
    try
    {
      var session = new ClientSession(client, _serviceProvider);
      await session.StartAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"클라이언트 세션 처리 중 오류: {ex.Message}");
      client.Dispose();
    }
  }

  public async Task StopAsync()
  {
    _serverCts.Cancel();
    _tcpListener.Stop();

    // 모든 활성 세션 종료 대기
    var activeSessions = _clientManager.GetActiveSessions();
    var closeTasks = activeSessions.Select(session => Task.Run(() => session.Dispose()));
    await Task.WhenAll(closeTasks);

    _serverCts.Dispose();
    Console.WriteLine("[Server] 서버가 정상적으로 종료되었습니다.");
  }
}