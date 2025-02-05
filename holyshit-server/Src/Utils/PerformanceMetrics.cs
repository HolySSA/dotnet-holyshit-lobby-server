using System.Text;

public class PerformanceMetrics
{
  private static readonly Dictionary<string, List<long>> _metrics = new();
  private static readonly object _lock = new();
  private static readonly string LogPath = "performance_metrics.log";

  public static async Task MeasureAsync(string operation, Func<Task> action)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await action();
    sw.Stop();

    lock (_lock)
    {
      if (!_metrics.ContainsKey(operation))
        _metrics[operation] = new List<long>();
      _metrics[operation].Add(sw.ElapsedMilliseconds);
    }

    // 측정할 때마다 파일에 저장
    SaveMetricsToFile();
  }

  public static Dictionary<string, (double avg, long min, long max, int count)> GetMetrics()
  {
    var result = new Dictionary<string, (double avg, long min, long max, int count)>();

    lock (_lock)
    {
      foreach (var metric in _metrics)
      {
        var values = metric.Value;
        result[metric.Key] = (
          avg: values.Average(),
          min: values.Min(),
          max: values.Max(),
          count: values.Count
        );
      }
    }

    return result;
  }

  private static void SaveMetricsToFile()
  {
    try
    {
      var metrics = GetMetrics();
      var logContent = new StringBuilder();
      logContent.AppendLine($"=== Performance Metrics at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

      foreach (var metric in metrics)
      {
        logContent.AppendLine($"Operation: {metric.Key}");
        logContent.AppendLine($"Average: {metric.Value.avg:F2}ms");
        logContent.AppendLine($"Min: {metric.Value.min}ms");
        logContent.AppendLine($"Max: {metric.Value.max}ms");
        logContent.AppendLine($"Count: {metric.Value.count}");
        logContent.AppendLine();
      }

      // 파일에 추가 (기존 내용 유지)
      File.AppendAllText(LogPath, logContent.ToString());
    }
    catch (Exception ex)
    {
      Console.WriteLine($"메트릭스 저장 중 오류 발생: {ex.Message}");
    }
  }

  // 메트릭스 초기화 메서드 (필요한 경우 사용)
  public static void Reset()
  {
    lock (_lock)
    {
      _metrics.Clear();
    }
  }
}