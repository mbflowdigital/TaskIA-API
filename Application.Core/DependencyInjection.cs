using Application.Core.Interfaces.Services;
using Application.Core.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Core;

/// <summary>
/// Configuração de injeção de dependência da camada Application
/// Registra Services e Validators
/// Seguindo Dependency Inversion Principle - Registra interfaces, não implementações
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        // Registrar validators do FluentValidation automaticamente
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Services - Registrando interface -> implementação (Dependency Inversion)
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAuthService, AuthService>();
        
        // Adicione outros services aqui seguindo o mesmo padrão
        // services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
