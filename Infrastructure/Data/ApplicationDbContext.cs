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
    public DbSet<Project> Projects { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar relacionamento User -> Projects
        modelBuilder.Entity<User>()
            .HasMany(u => u.Projects)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Não permite deletar usuário se tiver projetos

        // Índices para performance
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.UserId);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        // Configurações adicionais se necessário
        // Exemplo: modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();
    }
}
