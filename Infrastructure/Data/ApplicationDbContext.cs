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
    public DbSet<ProjectExecutionSettings> ProjectExecutionSettings { get; set; } = null!;
    public DbSet<ProjectPriorityRanking> ProjectPriorityRankings { get; set; } = null!;
    public DbSet<ProjectDependencies> ProjectDependencies { get; set; } = null!;
    public DbSet<ProjectIntegrations> ProjectIntegrations { get; set; } = null!;
    public DbSet<ProjectSensitiveData> ProjectSensitiveData { get; set; } = null!;
    public DbSet<Parameter> Parameters { get; set; } = null!;

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
                .OnDelete(DeleteBehavior.SetNull); 

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

        // Colunas condicionais de ProjectDetails
        modelBuilder.Entity<ProjectDetails>()
            .Property(pd => pd.ValorOrcamento)
            .HasColumnType("decimal(18,2)");

        // ========================================================================
        // Configuração de ProjectExecutionSettings e ProjectPriorityRanking
        // ========================================================================

        // Relacionamento Project -> ProjectExecutionSettings (1:1)
        modelBuilder.Entity<ProjectExecutionSettings>(entity =>
        {
            entity.ToTable("ProjectExecutionSettings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId)
                .IsRequired();

            entity.Property(e => e.MaiorRisco)
                .HasMaxLength(500);

            entity.Property(e => e.ExperienciaEquipe)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.NivelDetalhePlano)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.FrequenciaRevisao)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Observacoes)
                .HasMaxLength(1000);

            entity.Property(e => e.OQueDeuCerto)
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            entity.Property(e => e.OQueDeuErrado)
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            // Um projeto só pode ter um ProjectExecutionSettings
            entity.HasIndex(e => e.ProjectId)
                .IsUnique()
                .HasDatabaseName("IX_ProjectExecutionSettings_ProjectId");

            entity.HasOne(e => e.Project)
                .WithOne(p => p.ExecutionSettings)
                .HasForeignKey<ProjectExecutionSettings>(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Relacionamento Project -> ProjectPriorityRankings (1:N)
        modelBuilder.Entity<ProjectPriorityRanking>(entity =>
        {
            entity.ToTable("ProjectPriorityRankings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId)
                .IsRequired();

            entity.Property(e => e.PriorityType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Posicao)
                .IsRequired();

            // Garante que um tipo de prioridade não seja duplicado por projeto
            entity.HasIndex(e => new { e.ProjectId, e.PriorityType })
                .IsUnique()
                .HasDatabaseName("IX_ProjectPriorityRankings_ProjectId_PriorityType");

            // Índice para performance de ordenação
            entity.HasIndex(e => new { e.ProjectId, e.Posicao })
                .HasDatabaseName("IX_ProjectPriorityRankings_ProjectId_Posicao");

            entity.HasOne(e => e.Project)
                .WithMany(p => p.PriorityRankings)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================================================
        // Configuração de ProjectDependencies, ProjectIntegrations e ProjectSensitiveData
        // ========================================================================

        // Relacionamento Project -> ProjectDependencies (1:N)
        modelBuilder.Entity<ProjectDependencies>(entity =>
        {
            entity.ToTable("ProjectDependencies");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId)
                .IsRequired();

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Descricao)
                .HasMaxLength(500);

            entity.Property(e => e.Prazo)
                .IsRequired();

            entity.Property(e => e.Criticidade)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_ProjectDependencies_ProjectId");

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Dependencies)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Relacionamento Project -> ProjectIntegrations (1:N)
        modelBuilder.Entity<ProjectIntegrations>(entity =>
        {
            entity.ToTable("ProjectIntegrations");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId)
                .IsRequired();

            entity.Property(e => e.NomeSistema)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Tipo)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Criticidade)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_ProjectIntegrations_ProjectId");

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Integrations)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Relacionamento Project -> ProjectSensitiveData (1:N)
        modelBuilder.Entity<ProjectSensitiveData>(entity =>
        {
            entity.ToTable("ProjectSensitiveData");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId)
                .IsRequired();

            entity.Property(e => e.TipoDadoSensivel)
                .IsRequired()
                .HasConversion<int>();

            // Garante que um tipo de dado sensível não seja duplicado por projeto
            entity.HasIndex(e => new { e.ProjectId, e.TipoDadoSensivel })
                .IsUnique()
                .HasDatabaseName("IX_ProjectSensitiveData_ProjectId_TipoDadoSensivel");

            entity.HasOne(e => e.Project)
                .WithMany(p => p.SensitiveData)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================================================
        // Configuração de Parameter (tabela de parâmetros do sistema)
        // Apenas 2 colunas: Nome (PK) e Valor
        // ========================================================================
        modelBuilder.Entity<Parameter>(entity =>
        {
            entity.ToTable("Parameters");

            entity.HasKey(p => p.Nome);

            entity.Property(p => p.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Valor)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasData(new Parameter
            {
                Nome = "Prompt_Base",
                Valor = @"Você é um consultor sênior especialista em gerenciamento de projetos, análise de riscos e planejamento estratégico. 
Sua missão é analisar profundamente o projeto descrito abaixo e fornecer insights valiosos, identificando:
- Viabilidade real considerando recursos, prazos e complexidade
- Riscos técnicos, humanos, de negócio e externos
- Recomendações práticas e acionáveis para maximizar o sucesso do projeto

Considere o perfil da equipe, suas experiências passadas, restrições operacionais e o contexto completo do projeto.
Seja específico, objetivo e estratégico nas suas análises.

═══════════════════════════════════════════════════════════════════
📋 INFORMAÇÕES GERAIS DO PROJETO
═══════════════════════════════════════════════════════════════════

Nome do Projeto: {ProjectName}
Objetivo: {Objective}
Descrição Detalhada: {Description}

📅 Cronograma:
  • Data de Início: {StartDate}
  • Data de Término: {EndDate}

🏢 Contexto Organizacional:
  • Empresa: {Company}
  • Departamento: {Department}
  • Tipo de Projeto: {ProjectType}

═══════════════════════════════════════════════════════════════════
👥 COMPOSIÇÃO DA EQUIPE E RESPONSABILIDADES
═══════════════════════════════════════════════════════════════════

{TeamMembers}

💡 ANÁLISE REQUERIDA: Avalie se a equipe possui:
  - Seniority adequada para o escopo
  - Dedicação suficiente considerando o prazo
  - Papéis bem distribuídos (evitando sobrecarga)
  - Aprovadores estrategicamente posicionados

═══════════════════════════════════════════════════════════════════
⚙️ CONTEXTO OPERACIONAL E RESTRIÇÕES
═══════════════════════════════════════════════════════════════════

💰 Orçamento:
  {Budget}

⏰ Regime de Trabalho:
  {WorkSchedule}

🚨 Política de Downtime:
  {DowntimePolicy}

🔗 Dependências Externas:
{ExternalDependencies}

🔌 Integrações Necessárias:
{Integrations}

📜 Conformidade e Regulamentações:
  • Requisitos: {Compliance}
  • Aprovadores de Compliance: {ComplianceApprovers}

🚫 Períodos de Indisponibilidade da Equipe:
{UnavailablePeriods}

💡 ANÁLISE REQUERIDA: Identifique:
  - Dependências críticas que podem bloquear o projeto
  - Conflitos entre prazos de dependências e cronograma
  - Riscos de disponibilidade da equipe em fases críticas
  - Impacto das políticas de downtime na estratégia de deploy

═══════════════════════════════════════════════════════════════════
🎯 PRIORIDADES E CONTEXTO ESTRATÉGICO
═══════════════════════════════════════════════════════════════════

Ranking de Prioridades: {PriorityRanking}

⚠️ Maior Risco Percebido pela Equipe:
{BiggestRisk}

📚 Experiência Prévia da Equipe:
  • Nível de experiência: {PreviousExperience}
  • O que funcionou bem em projetos anteriores: {WhatWentWell}
  • O que não funcionou em projetos anteriores: {WhatWentWrong}

📊 Expectativas de Gestão:
  • Nível de Detalhe no Planejamento: {DetailLevel}
  • Frequência de Revisão: {ReviewFrequency}

📝 Observações Finais do Solicitante:
{FinalObservations}

💡 ANÁLISE REQUERIDA:
  - Compare as prioridades declaradas com os riscos identificados
  - Identifique se a experiência prévia da equipe é compatível com os desafios
  - Sugira ajustes no nível de detalhe ou frequência de revisão se necessário
  - Considere as lições aprendidas para evitar erros recorrentes

═══════════════════════════════════════════════════════════════════
📤 FORMATO DE RESPOSTA OBRIGATÓRIO
═══════════════════════════════════════════════════════════════════

Responda SOMENTE no formato JSON abaixo (sem markdown, sem explicações adicionais):

{
  ""overview"": ""Análise geral do projeto incluindo: viabilidade técnica e de negócio, pontos fortes da proposta, principais desafios identificados, adequação da equipe ao escopo, e uma avaliação crítica do cronograma proposto. Seja objetivo e direto (3-4 frases)."",
  ""risks"": ""Riscos no formato: CRITICO: <lista separada por vírgula> | ALTO: <lista separada por vírgula> | MEDIO: <lista separada por vírgula> | BAIXO: <lista separada por vírgula>. Classifique cada risco (técnico, de equipe, de negócio, de cronograma) no nível adequado. Use cada nível apenas se houver riscos reais nele; omita os que não se aplicam. Seja específico e cite exemplos do contexto fornecido."",
  ""recommendations"": ""Forneça recomendações práticas e acionáveis separadas por ponto e vírgula. Inclua sugestões para: mitigação de riscos identificados, otimização da alocação da equipe, estratégias de gestão de dependências, melhorias no processo de aprovação, pontos de atenção no cronograma, e boas práticas baseadas nas experiências anteriores relatadas. Seja estratégico e prático.""
}"
            });
        });
    }
}
