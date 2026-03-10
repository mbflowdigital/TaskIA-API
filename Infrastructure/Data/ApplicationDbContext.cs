using Domain.Entities;
using Domain.Enums;
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
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<RoleEntity> Roles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relacionamento Company -> Users (1:N)
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Users)
            .WithOne(u => u.Company)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(r => r.Id);

            // ID não auto-incrementa, usa valores do enum UserRole
            entity.Property(r => r.Id)
                .ValueGeneratedNever();

            // Seed inicial com os valores do enum
            entity.HasData(
                new RoleEntity { Id = (int)UserRole.USER, RoleName = "USER", Description = "Usuário padrão do sistema" },
                new RoleEntity { Id = (int)UserRole.ADM, RoleName = "ADM", Description = "Administrador da empresa" },
                new RoleEntity { Id = (int)UserRole.ADM_MASTER, RoleName = "ADM_MASTER", Description = "Administrador master do sistema" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Relacionamento com Role
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Define valor padrão para RoleId
            entity.Property(u => u.RoleId)
                .HasDefaultValue((int)UserRole.USER);
        });


        // Relacionamento Company -> Projects (1:N)
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Projects)
            .WithOne(p => p.Company)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.CompanyId);

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
