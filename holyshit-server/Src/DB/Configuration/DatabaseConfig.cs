using Holyshit.DB.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DatabaseConfig
{
  public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
  {
    // DB 컨텍스트 등록
    services.AddDbContext<ApplicationDbContext>(options =>
    {
      options.UseNpgsql(
        configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
          npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
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

      // 데이터베이스 생성 및 마이그레이션 적용
      await dbContext.Database.MigrateAsync();

      Console.WriteLine("데이터베이스 초기화 완료");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"데이터베이스 초기화 중 오류 발생: {ex.Message}");
      throw;
    }
  }
}