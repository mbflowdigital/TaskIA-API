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
    public DbSet<PositionsEntity> Positions { get; set; } = null!;
    public DbSet<ProjectMemberEntity> ProjectMembers { get; set; } = null;
    public DbSet<ProjectDetails> ProjectDetails { get; set; } = null!;
    public DbSet<ProjectCompliance> ProjectCompliances { get; set; } = null!;
    public DbSet<ProjectUnavailablePeriod> ProjectUnavailablePeriods { get; set; } = null!;

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

        // Configuração de Positions com seed
        modelBuilder.Entity<PositionsEntity>(entity =>
        {
            entity.ToTable("Positions");

            entity.HasKey(p => p.Id);

            // ID não auto-incrementa, usa valores do enum UserPosition
            entity.Property(p => p.Id)
                .ValueGeneratedNever();

            // Seed inicial com posições comuns
            entity.HasData(
                new PositionsEntity { Id = (int)UserPosition.Developer, PositionName = "Desenvolvedor", Description = "Desenvolvedor de software" },
                new PositionsEntity { Id = (int)UserPosition.Designer, PositionName = "Designer", Description = "Designer UI/UX" },
                new PositionsEntity { Id = (int)UserPosition.ProjectManager, PositionName = "Gerente de Projeto", Description = "Gerente de projeto" },
                new PositionsEntity { Id = (int)UserPosition.ProductOwner, PositionName = "Product Owner", Description = "Dono do produto" },
                new PositionsEntity { Id = (int)UserPosition.ScrumMaster, PositionName = "Scrum Master", Description = "Facilitador Scrum" },
                new PositionsEntity { Id = (int)UserPosition.QA, PositionName = "QA", Description = "Analista de qualidade" },
                new PositionsEntity { Id = (int)UserPosition.DevOps, PositionName = "DevOps", Description = "Engenheiro DevOps" },
                new PositionsEntity { Id = (int)UserPosition.Analyst, PositionName = "Analista", Description = "Analista de sistemas" },
                new PositionsEntity { Id = (int)UserPosition.Other, PositionName = "Outro", Description = "Outra posição" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Relacionamento com Role
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamento com Position
            entity.HasOne(u => u.Position)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PositionId)
                .OnDelete(DeleteBehavior.SetNull); // Permite null

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

        // Configurar relacionamento Project -> ProjectMembers (1:N)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.ProjectMembers)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // Deleta membros ao deletar projeto

        // Configurar relacionamento User -> ProjectMembers (1:N)
        modelBuilder.Entity<User>()
            .HasMany<ProjectMemberEntity>()
            .WithOne(pm => pm.User)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Não permite deletar usuário se for membro de projeto

        // Índice composto para garantir que um usuário não seja adicionado duas vezes ao mesmo projeto
        modelBuilder.Entity<ProjectMemberEntity>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // Índices para performance
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.UserId);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        // ========================================================================
        // Configuração de ProjectDetails e relacionamentos
        // ========================================================================

        // Relacionamento Project -> ProjectDetails (1:1)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.ProjectDetails)
            .WithOne(pd => pd.Project)
            .HasForeignKey<ProjectDetails>(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // Deleta detalhes ao deletar projeto

        // Relacionamento ProjectDetails -> ProjectCompliance (1:N)
        modelBuilder.Entity<ProjectDetails>()
            .HasMany(pd => pd.Compliances)
            .WithOne(pc => pc.ProjectDetails)
            .HasForeignKey(pc => pc.ProjectDetailsId)
            .OnDelete(DeleteBehavior.Cascade); // Deleta compliances ao deletar detalhes

        // Relacionamento ProjectDetails -> ProjectUnavailablePeriod (1:N)
        modelBuilder.Entity<ProjectDetails>()
            .HasMany(pd => pd.UnavailablePeriods)
            .WithOne(pu => pu.ProjectDetails)
            .HasForeignKey(pu => pu.ProjectDetailsId)
            .OnDelete(DeleteBehavior.Cascade); // Deleta períodos ao deletar detalhes

        // Índice composto para garantir que um tipo de compliance não seja adicionado duas vezes ao mesmo projeto
        modelBuilder.Entity<ProjectCompliance>()
            .HasIndex(pc => new { pc.ProjectDetailsId, pc.TipoCompliance });

        // Índices para ProjectDetails
        modelBuilder.Entity<ProjectDetails>()
            .HasIndex(pd => pd.ProjectId)
            .IsUnique(); // Um projeto só pode ter um ProjectDetails

        // Índices para ProjectUnavailablePeriod
        modelBuilder.Entity<ProjectUnavailablePeriod>()
            .HasIndex(pu => new { pu.ProjectDetailsId, pu.DataInicio, pu.DataFim });

        // Configuração de enums como int no banco
        modelBuilder.Entity<ProjectDetails>()
            .Property(pd => pd.Orcamento)
            .HasConversion<int>();

        modelBuilder.Entity<ProjectDetails>()
            .Property(pd => pd.HorarioTrabalho)
            .HasConversion<int>();

        modelBuilder.Entity<ProjectDetails>()
            .Property(pd => pd.DowntimePermitido)
            .HasConversion<int>();

        modelBuilder.Entity<ProjectCompliance>()
            .Property(pc => pc.TipoCompliance)
            .HasConversion<int>();

        // Configurações adicionais se necessário
        // Exemplo: modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();
    }
}
