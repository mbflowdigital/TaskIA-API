using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Dependências externas do projeto.
/// Só deve existir quando ProjectDetails.TemDependenciasExternas = true.
/// Relação 1:N com Project via ProjectId.
/// </summary>
public class ProjectDependencies : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required]
    public DateTime Prazo { get; set; }

    [Required]
    [MaxLength(100)]
    public string Criticidade { get; set; } = string.Empty;

    public ProjectDependencies() { }

    /// <summary>
    /// Atualiza os dados da dependência externa
    /// </summary>
    public void UpdateInfo(string nome, string? descricao, DateTime prazo, string criticidade)
    {
        Nome = nome;
        Descricao = descricao;
        Prazo = prazo;
        Criticidade = criticidade;
        SetUpdatedAt();
    }
}
