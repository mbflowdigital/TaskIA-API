using CrossCutting.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrossCutting(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseCrossCutting(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configurar tratamento global de exceções
        app.ConfigureExceptionHandler(env);
        
        return app;
    }
}
