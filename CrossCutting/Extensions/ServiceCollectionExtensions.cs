using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrossCutting(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseCrossCutting(this IApplicationBuilder app)
    {
        return app;
    }
}
