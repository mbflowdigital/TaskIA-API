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
        var validStatuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled", "Inactive" };
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
}
