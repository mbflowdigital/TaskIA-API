using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Dados sensíveis tratados pelo projeto.
/// Não deve existir se ProjectCompliance contiver TipoCompliance = DadosPublicos.
/// Relação 1:N com Project via ProjectId.
/// </summary>
public class ProjectSensitiveData : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [Required]
    public SensitiveDataType TipoDadoSensivel { get; set; }

    public ProjectSensitiveData() { }
}
