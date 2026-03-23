using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade de Período Indisponível do Projeto
/// Representa janelas de tempo onde o projeto não pode sofrer alterações
/// </summary>
public class ProjectUnavailablePeriod : BaseEntity
{
    // Relacionamento com ProjectDetails
    [Required]
    public Guid ProjectDetailsId { get; set; }

    [ForeignKey(nameof(ProjectDetailsId))]
    public virtual ProjectDetails? ProjectDetails { get; set; }

    // Período Indisponível
    [Required]
    public DateTime DataInicio { get; set; }

    [Required]
    public DateTime DataFim { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;

    // Construtor público
    public ProjectUnavailablePeriod() { }

    /// <summary>
    /// Atualiza informações do período indisponível
    /// </summary>
    public void UpdateInfo(DateTime dataInicio, DateTime dataFim, string motivo)
    {
        if (dataFim < dataInicio)
            throw new ArgumentException("Data fim não pode ser anterior à data início");

        DataInicio = dataInicio;
        DataFim = dataFim;
        Motivo = motivo;
        SetUpdatedAt();
    }

    /// <summary>
    /// Verifica se o período está ativo (não passou ainda)
    /// </summary>
    public bool IsPeriodActive()
    {
        var now = DateTime.UtcNow;
        return DataInicio <= now && now <= DataFim;
    }

    /// <summary>
    /// Verifica se uma data está dentro do período indisponível
    /// </summary>
    public bool IsDateInPeriod(DateTime date)
    {
        return date >= DataInicio && date <= DataFim;
    }
}
