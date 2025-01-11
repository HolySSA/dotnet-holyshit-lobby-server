using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Core.Client;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using HolyShitServer.Src.Services.LoadBalancing;

namespace HolyShitServer.Src.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 인프라 서비스 등록
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis 설정
        var redisConnection = configuration["Redis:ConnectionString"];
        if (string.IsNullOrEmpty(redisConnection))
        {
            throw new InvalidOperationException("Redis connection string is not configured");
        }
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<RedisService>();
        
        // DB 설정
        services.AddDbContext<ApplicationDbContext>();
        return services;
    }

    /// <summary>
    /// 게임 서비스 등록
    /// </summary>
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<GameDataManager>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddSingleton<ClientManager>();
        services.AddSingleton<MessageQueueService>();
        return services;
    }

    /// <summary>
    /// 로드밸런싱 서비스 등록
    /// </summary>
    public static IServiceCollection AddLoadBalancing(this IServiceCollection services)
    {
        services.AddSingleton<IServerSelectionStrategy, RoundRobinStrategy>();
        services.AddSingleton<LoadBalancer>();
        return services;
    }
} 