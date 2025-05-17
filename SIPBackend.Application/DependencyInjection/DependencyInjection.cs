using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SIPBackend.Application.Data;
using SIPBackend.Application.Interfaces;
using SIPBackend.Application.Services;

namespace SIPBackend.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<Initializer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRelationshipsService, RelationshipsService>();
        
        return services;
    }
}