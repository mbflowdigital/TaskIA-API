using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace CrossCutting.Middleware;

/// <summary>
/// Middleware global para tratamento de exceções
/// Captura erros não tratados e retorna respostas padronizadas
/// </summary>
public static class ExceptionHandlingMiddleware
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                
                if (exceptionHandlerFeature != null)
                {
                    var exception = exceptionHandlerFeature.Error;
                    
                    // Log detalhado em desenvolvimento
                    var errorDetails = new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Title = "Ocorreu um erro interno no servidor",
                        Detail = env.EnvironmentName == Environments.Development
                            ? $"{exception.Message}\n\nStackTrace: {exception.StackTrace}" 
                            : "Entre em contato com o suporte se o problema persistir.",
                        Instance = context.Request.Path
                    };

                    // TODO: Adicionar logging aqui (Serilog, Application Insights, etc)
                    // _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);

                    await context.Response.WriteAsJsonAsync(errorDetails);
                }
            });
        });
    }
}
