using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade de Projeto
/// Representa um projeto gerenciado pelo sistema
/// </summary>
public class Project : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Objective { get; set; }

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Active, Paused, Completed, Cancelled, Inactive

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(200)]
    public string? ResponsibleSector { get; set; }

    [MaxLength(100)]
    public string? ProjectType { get; set; }

    public string? Prompt_enviado { get; set; }

    // Campos para armazenar análise da IA
    public string? IA_Overview { get; set; }

    public string? IA_Risks { get; set; }

    public string? IA_Recommendations { get; set; }

    // Relacionamento com User
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    // Relacionamento com Company (escopo multi-tenant)
    public Guid? CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    // Relacionamento com ProjectMembers (membros da equipe do projeto)
    public virtual ICollection<ProjectMemberEntity> ProjectMembers { get; set; } = new List<ProjectMemberEntity>();

    // Relacionamento com ProjectDetails (detalhes e configurações do projeto)
    public virtual ProjectDetails? ProjectDetails { get; set; }

    // Relacionamento com ProjectExecutionSettings (prioridades e expectativas do projeto)
    public virtual ProjectExecutionSettings? ExecutionSettings { get; set; }

    // Relacionamento com ProjectPriorityRankings (ranking de prioridades do projeto)
    public virtual ICollection<ProjectPriorityRanking> PriorityRankings { get; set; } = new List<ProjectPriorityRanking>();

    // Relacionamento com ProjectDependencies (dependências externas — ativo quando TemDependenciasExternas = true)
    public virtual ICollection<ProjectDependencies> Dependencies { get; set; } = new List<ProjectDependencies>();

    // Relacionamento com ProjectIntegrations (integrações — ativo quando TemIntegracoes = true)
    public virtual ICollection<ProjectIntegrations> Integrations { get; set; } = new List<ProjectIntegrations>();

    // Relacionamento com ProjectSensitiveData (dados sensíveis tratados pelo projeto)
    public virtual ICollection<ProjectSensitiveData> SensitiveData { get; set; } = new List<ProjectSensitiveData>();

    // Relacionamento com ProjectTasks (tarefas do projeto)
    public virtual ICollection<Board> Tasks { get; set; } = new List<Board>();

    // Construtor p�blico
    public Project() { }

    /// <summary>
    /// Atualiza informa��es b�sicas do projeto
    /// </summary>
    public void UpdateInfo(string name, string? objective, string? description, DateTime? startDate, DateTime? endDate, string? responsibleSector = null, string? projectType = null)
    {
        Name = name;
        Objective = objective;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        ResponsibleSector = responsibleSector;
        ProjectType = projectType;
        SetUpdatedAt();
    }

    /// <summary>
    /// Atualiza status do projeto
    /// </summary>
    public void UpdateStatus(string status)
    {
        if (IsValidStatus(status))
        {
            Status = status;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Inativa o projeto (altera status para Inactive, mas mant�m IsActive = true)
    /// </summary>
    public void SetInactive()
    {
        Status = "Inactive";
        SetUpdatedAt();
    }

    /// <summary>
    /// Cancela o projeto (altera status para Cancelled e desativa)
    /// </summary>
    public void Cancel()
    {
        Status = "Cancelled";
        Deactivate();
        SetUpdatedAt();
    }

    /// <summary>
    /// Valida se o status � v�lido
    /// </summary>
    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled", "Inactive", "Waiting_Approve" };
        return validStatuses.Contains(status);
    }

    /// <summary>
    /// Soft delete do projeto
    /// </summary>
    public void SoftDelete()
    {
        Deactivate();
        SetUpdatedAt();
    }

    /// <summary>
    /// Atualiza os resultados da análise da IA
    /// </summary>
    public void UpdateAnalysisResults(string? overview, string? risks, string? recommendations)
    {
        IA_Overview = overview;
        IA_Risks = risks;
        IA_Recommendations = recommendations;
        SetUpdatedAt();
    }

    /// <summary>
    /// Adiciona um membro ao projeto
    /// </summary>
    public void AddMember(ProjectMemberEntity member)
    {
        ProjectMembers.Add(member);
        SetUpdatedAt();
    }
    
    /// <summary>
    /// Remove um membro do projeto
    /// </summary>
    public void RemoveMember(Guid memberId)
    {
        var member = ProjectMembers.FirstOrDefault(m => m.Id == memberId);
        if (member != null)
        {
            member.Deactivate();
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Remove uma dependência externa do projeto
    /// </summary>
    public void RemoveDependency(Guid dependencyId)
    {
        var dependency = Dependencies.FirstOrDefault(d => d.Id == dependencyId);
        if (dependency != null)
        {
            dependency.Deactivate();
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Remove uma integração do projeto
    /// </summary>
    public void RemoveIntegration(Guid integrationId)
    {
        var integration = Integrations.FirstOrDefault(i => i.Id == integrationId);
        if (integration != null)
        {
            integration.Deactivate();
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Remove um dado sensível do projeto
    /// </summary>
    public void RemoveSensitiveData(Guid sensitiveDataId)
    {
        var data = SensitiveData.FirstOrDefault(s => s.Id == sensitiveDataId);
        if (data != null)
        {
            data.Deactivate();
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Atualiza o prompt enviado para a IA
    /// </summary>
    public void UpdatePromptEnviado(string prompt)
    {
        Prompt_enviado = prompt;
        SetUpdatedAt();
    }
}
