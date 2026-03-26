using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Integrações externas do projeto.
/// Só deve existir quando ProjectDetails.TemIntegracoes = true.
/// Relação 1:N com Project via ProjectId.
/// </summary>
public class ProjectIntegrations : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [Required]
    [MaxLength(200)]
    public string NomeSistema { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Criticidade { get; set; } = string.Empty;

    [Required]
    public IntegrationStatusType Status { get; set; } = IntegrationStatusType.Criar;

    public ProjectIntegrations() { }

    /// <summary>
    /// Atualiza os dados da integração
    /// </summary>
    public void UpdateInfo(string nomeSistema, string tipo, string criticidade, IntegrationStatusType status)
    {
        NomeSistema = nomeSistema;
        Tipo = tipo;
        Criticidade = criticidade;
        Status = status;
        SetUpdatedAt();
    }
}
