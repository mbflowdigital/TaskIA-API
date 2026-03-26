using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade de Membros do Projeto
/// Representa os usuários que fazem parte de um projeto
/// </summary>
public class ProjectMemberEntity : BaseEntity
{
    [MaxLength(100)]
    public string? ProjectFunction { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Dedication { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Approver { get; set; }

    [MaxLength(500)]
    public string? FunctionDescription { get; set; }

    // Relacionamento com Project
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    // Relacionamento com User
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    // Construtor público
    public ProjectMemberEntity() { }

    /// <summary>
    /// Atualiza informações do membro
    /// </summary>
    public void UpdateInfo(string? projectFunction, string? dedication, string? approver, string? functionDescription)
    {
        ProjectFunction = projectFunction;
        Dedication = dedication;
        Approver = approver;
        FunctionDescription = functionDescription;
        SetUpdatedAt();
    }
}
