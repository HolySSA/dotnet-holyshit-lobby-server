using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HolyShitServer.Src.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis 설정
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is not configured")));

        // DB Context
        services.AddDbContext<ApplicationDbContext>();

        // Token Validation Service
        services.AddSingleton<TokenValidationService>();

        // Game Data Manager
        services.AddSingleton<GameDataManager>();

        return services;
    }
} 