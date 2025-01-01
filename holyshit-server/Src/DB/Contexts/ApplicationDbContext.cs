using HolyShitServer.Src.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HolyShitServer.DB.Contexts;

// DbContext 상속, 데이터베이스 컨텍스트 클래스 생성
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCharacter> UserCharacters { get; set; } = null!;
    
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

    // 모델 생성 시 추가 설정
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 기본 설정
        base.OnModelCreating(modelBuilder);

        // UserCharacter 테이블 설정
        modelBuilder.Entity<UserCharacter>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // 기존 Users 테이블과 연결
            entity.HasOne(e => e.User)
                .WithMany(u => u.Characters)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // UserId와 CharacterType의 조합은 유니크.
            entity.HasIndex(e => new { e.UserId, e.CharacterType }).IsUnique();

            // PurchasedAt은 기본값을 현재 시간으로 설정
            entity.Property(e => e.PurchasedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            // UpdatedAt은 기본값을 현재 시간으로 설정하고, 업데이트 시 자동 업데이트
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnUpdate();
        });
    }
}