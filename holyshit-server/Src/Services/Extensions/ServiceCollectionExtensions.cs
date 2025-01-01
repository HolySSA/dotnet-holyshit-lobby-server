using HolyShitServer.Src.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        // 다른 서비스들도 여기에 추가...
        
        return services;
    }
} 