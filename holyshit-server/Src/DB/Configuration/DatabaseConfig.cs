using HolyShitServer.DB.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.DB.Configuration;

public static class DatabaseConfig
{
  public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");

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

  public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
  {
    try
    {
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 데이터베이스 연결 테스트만 수행
      await dbContext.Database.CanConnectAsync();

      Console.WriteLine("데이터베이스 초기화 완료");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"데이터베이스 초기화 중 오류 발생: {ex.Message}");
      throw;
    }
  }
}