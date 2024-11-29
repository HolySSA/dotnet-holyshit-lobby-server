using System.Net;
using System.Net.Sockets;
using HolyShitServer.Src.Constants;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Network;

namespace HolyShitServer;

class Program
{
  private static TcpListener? _tcpListener; // TCP 서버 객체
  private static readonly CancellationTokenSource _serverCts = new(); // 서버 종료 토큰 (서버 관리)
  private static readonly List<TcpClientHandler> _activeClients = new(); // 연결된 클라이언트 목록
  private static readonly object _clientLock = new(); // 클라이언트 목록 동시 접근 제한 객체

  public static async Task Main()
  {
    // 서버 종료 시 실행되는 이벤트 핸들러
    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

    try
    {
      await InitializeServerAsync(); // 서버 초기화
      await StartTcpServerAsync(); // TCP 서버 시작
    }
    catch (Exception ex)
    {
      Console.WriteLine($"서버 시작 중 오류: {ex.Message}");
    }
    finally
    {
      await CleanupAsync(); // 서버 정리(리소스)
    }
  }

  private static async Task InitializeServerAsync()
  {
    var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 초기화 시간 측정 스톱워치

    try
    {
      // 게임 데이터 로드
      var gameDataManager = new GameDataManager();
      await gameDataManager.InitializeDataAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"서버 초기화 중 오류: {ex.Message}");
      throw;
    }

    stopwatch.Stop();
    Console.WriteLine($"초기화 완료: {stopwatch.ElapsedMilliseconds}ms");
  }

  private static async Task StartTcpServerAsync()
  {
    // TCP 서버 객체 생성 및 바인딩
    _tcpListener = new TcpListener(IPAddress.Parse(ServerConstants.HOST), ServerConstants.PORT);
    _tcpListener.Start(); // 서버 시작
    Console.WriteLine($"TCP 서버 - {ServerConstants.HOST}:{ServerConstants.PORT}");

    // 종료 토큰 체크 - 서버 실행 유지
    while (!_serverCts.Token.IsCancellationRequested)
    {
      try
      {
        var client = await _tcpListener.AcceptTcpClientAsync(_serverCts.Token); // 클라이언트 연결 대기(수락)
        _ = HandleClientAsync(client); // 클라이언트 연결 처리  
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

  private static async Task HandleClientAsync(TcpClient client)
  {
    TcpClientHandler? handler = null; // 클라이언트 핸들러 객체 
    var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "알 수 없는 클라이언트";

    try
    {
      handler = new TcpClientHandler(client); // 클라이언트 핸들러 객체 생성

      lock (_clientLock)
      {
        _activeClients.Add(handler); // 연결된 클라이언트 목록 추가
      }

      // 데이터 수신 이벤트 핸들러 등록
      handler.onDataReceived += async (data) =>
      {
        await Task.Run(() =>
        {
          Console.WriteLine($"수신한 데이터: {data}");

          // 비동기로 데이터 처리 로직
        });
      };

      await handler.StartHandlingClientAsync(); // 클라이언트 핸들러 처리 시작
    }
    catch (Exception ex)
    {
      Console.WriteLine($"클라이언트 처리 중 오류: {ex.Message}");
    }
    finally
    {
      if (handler != null)
      {
        lock (_clientLock)
        {
          _activeClients.Remove(handler); // 연결된 클라이언트 목록 제거
        }

        handler.Dispose(); // 클라이언트 핸들러 객체 해제
      }
    }
  }

  private static void OnProcessExit(object? sender, EventArgs e)
  {
    _serverCts.Cancel(); // 서버 종료 토큰 발생 
    Task.Run(CleanupAsync).Wait(); // 리소스 정리
  }

  private static async Task CleanupAsync()
  {
    try
    {
      _tcpListener?.Stop(); // TCP 서버 객체 해제 - 서버 종료

      // 연결된 클라이언트 정리
      List<TcpClientHandler> clientsToDispose;
      lock (_clientLock)
      {
        clientsToDispose = new List<TcpClientHandler>(_activeClients);
        _activeClients.Clear();
      }

      // 모든 클라이언트 핸들러 비동기로 해제
      var disposeTasks = clientsToDispose.Select(client => Task.Run(() => client.Dispose())).ToList();
      await Task.WhenAll(disposeTasks); // 모든 클라이언트 핸들러 해제 완료 대기

      Console.WriteLine("모든 리소스 정리 완료.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"리소스 정리 중 오류: {ex.Message}");
    }
    finally
    {
      _serverCts.Dispose(); // 서버 종료 토큰 해제
    }
  }
}
