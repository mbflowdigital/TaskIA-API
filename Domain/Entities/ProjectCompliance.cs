using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Entidade de Compliance do Projeto
/// Representa os tipos de conformidade aplicados ao projeto
/// </summary>
public class ProjectCompliance : BaseEntity
{
    // Relacionamento com ProjectDetails
    [Required]
    public Guid ProjectDetailsId { get; set; }

    [ForeignKey(nameof(ProjectDetailsId))]
    public virtual ProjectDetails? ProjectDetails { get; set; }

    // Tipo de Compliance
    [Required]
    public ComplianceType TipoCompliance { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    // Construtor público
    public ProjectCompliance() { }

    /// <summary>
    /// Atualiza informações do compliance
    /// </summary>
    public void UpdateInfo(ComplianceType tipoCompliance, string? observacoes = null)
    {
        TipoCompliance = tipoCompliance;
        Observacoes = observacoes;
        SetUpdatedAt();
    }
}
