using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Contexto principal do Entity Framework Core
/// Usa Data Annotations das entidades para configuração
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    // TODO: Adicionar outros DbSets conforme necessário

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configurações adicionais se necessário (índices, query filters, etc)
        // Exemplo: modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();
    }
}
