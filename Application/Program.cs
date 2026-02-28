using Application.Core;
using CrossCutting.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configurar Forwarded Headers (CRÍTICO para HTTPS atrás de proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configurar CORS para frontends (Angular/React)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [
                "http://localhost:4200",
                "http://localhost:4201",
                "http://localhost:8080"
            ];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Adicionar camadas ao container de DI
builder.Services.AddApplicationCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCrossCutting();

// Configurar Controllers
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskIA API",
        Version = "v1",
        Description = "API construída com Clean Architecture e princípios SOLID",
        Contact = new OpenApiContact
        {
            Name = "Equipe TaskIA",
            Email = "contato@taskia.com"
        }
    });

    // Incluir comentários XML na documentação
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// CRÍTICO: UseForwardedHeaders DEVE vir PRIMEIRO
app.UseForwardedHeaders();

// Swagger disponível em TODOS os ambientes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskIA API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz
    c.DocumentTitle = "TaskIA API - Documentação";
});

// CORS
app.UseCors("AllowAngular");

// Middleware de exceções
app.UseCrossCutting(app.Environment);

app.UseAuthorization();
app.MapControllers();

app.Run();
