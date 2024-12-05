using System.Net.Sockets;

namespace HolyShitServer.Src.Core;

public class ServerManager
{
  private static TcpListener? _tcpListener; // TCP 서버 객체
  private static readonly CancellationTokenSource _serverCts = new(); // 서버 종료 토큰 (서버 관리)
  private readonly ClientManager _clientManager;
  private static IServiceProvider? _serviceProvider; // 서비스 프로바이더 객체
}