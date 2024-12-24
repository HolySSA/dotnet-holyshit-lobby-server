using HolyShitServer.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HolyShitServer.DB.Contexts;

// DbContext 상속, 데이터베이스 컨텍스트 클래스 생성
public class ApplicationDbContext : DbContext
{
    // 생성자 - 데이터베이스 연결 설정
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // 디자인 타임용 생성자
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    public DbSet<User> Users { get; set; } = null!; // null! - 프로퍼티가 null이 아님을 컴파일러에게 알림

    // 모델 생성 시 추가 설정
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 기본 설정
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)    // Email 컬럼 인덱스 생성
            .IsUnique();               // Email 중복 X

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Nickname) // Nickname 컬럼 인덱스 생성
            .IsUnique();               // Nickname 중복 X
    }
}