using Application.Core;
using Application.Core.Interfaces.Services;
using Application.Core.Services;
using CrossCutting.Extensions;
using Infrastructure;
using Infrastructure.ConfigurationJwtToken;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configurar Forwarded Headers (CR�TICO para HTTPS atr�s de proxy)
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
builder.Services.AddInfrastructure(builder.Configuration); // J� configura JWT Authentication
builder.Services.AddCrossCutting();

// Registrar HttpClient para UserService
builder.Services.AddHttpClient<IUserService, UserService>();

// Registrar ClaudeService com HttpClient dedicado
builder.Services.AddHttpClient<Infrastructure.Services.ClaudeService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5); // 5 minutos para análises complexas com IA
    });

// Configurar Controllers
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI com suporte JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskIA API",
        Version = "v1",
        Description = "API constru�da com Clean Architecture, JWT Bearer Authentication e princ�pios SOLID",
        Contact = new OpenApiContact
        {
            Name = "Equipe TaskIA",
            Email = "contato@taskia.com"
        }
    });

    // Configurar autentica��o Bearer no Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir coment�rios XML na documenta��o
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// CR�TICO: UseForwardedHeaders DEVE vir PRIMEIRO
app.UseForwardedHeaders();

// Swagger dispon�vel em TODOS os ambientes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskIA API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz
    c.DocumentTitle = "TaskIA API - Documenta��o";
});

// CORS
app.UseCors("AllowAngular");

// Middleware de exce��es
app.UseCrossCutting(app.Environment);

app.UseAuthentication(); // 1� - Valida o token JWT

app.UseMiddleware<TokenBlacklistMiddleware>(); // 2� - Verifica se token est� revogado

app.UseAuthorization(); // 3� - Verifica permiss�es/claims

app.MapControllers();

app.Run();
