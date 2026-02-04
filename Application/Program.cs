using Application.Core;
using CrossCutting.Extensions;
using Infrastructure;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201"
            )
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
            Name = "Seu Nome/Equipe",
            Email = "email@exemplo.com"
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

// Configure o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskIA API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

// CORS - Deve vir ANTES de UseAuthorization
app.UseCors("AllowAngular");

// Middleware de exceções (CrossCutting)
app.UseCrossCutting();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
