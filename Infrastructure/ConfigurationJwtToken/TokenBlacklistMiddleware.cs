using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Infrastructure.ConfigurationJwtToken;

/// <summary>
/// Middleware que valida se o token JWT está revogado
/// Usa JwtTokenService para verificar a blacklist
/// Deve ser executado APÓS o middleware de autenticação
/// </summary>
public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        // Extrair token do header Authorization
        var token = ExtractTokenFromHeader(context);

        if (!string.IsNullOrWhiteSpace(token))
        {
            // Verificar se o token está revogado usando JwtTokenService
            var isRevoked = await jwtTokenService.IsTokenRevokedAsync(token);

            if (isRevoked)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Token revogado ou inválido. Faça login novamente.",
                    isSuccess = false
                });
                
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extrai o token JWT do header Authorization
    /// </summary>
    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return null;
        }

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }
}