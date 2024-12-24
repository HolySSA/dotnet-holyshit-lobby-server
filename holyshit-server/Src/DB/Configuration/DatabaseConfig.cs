using HolyShitServer.DB.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.DB.Configuration;

public static class DatabaseConfig
{
  public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = BuildConnectionString();

    // DB 컨텍스트 등록
    services.AddDbContext<ApplicationDbContext>(options =>
    {
      options.UseNpgsql(connectionString, npgsqlOptions =>
      {
        npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: 3,
          maxRetryDelay: TimeSpan.FromSeconds(30),
          errorCodesToAdd: null
        );
      });
    });

    return services;
  }

  private static string BuildConnectionString()
  {
    return $"Host={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
           $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
           $"Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "holyshit_db"};" +
           $"Username={Environment.GetEnvironmentVariable("DB_USER") ?? "postgres"};" +
           $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "aaaa4321"};" +
           "Maximum Pool Size=100;Minimum Pool Size=0;";
  }

  public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
  {
    try
    {
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 마이그레이션 적용
      if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
      {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("마이그레이션 적용 완료");
      }

      Console.WriteLine("데이터베이스 초기화 완료");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"데이터베이스 초기화 중 오류 발생: {ex.Message}");
      throw;
    }
  }
}