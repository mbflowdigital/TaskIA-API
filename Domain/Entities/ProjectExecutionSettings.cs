using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Configurações de execução e expectativas do projeto.
/// Relação 1:1 com Project diretamente via ProjectId.
/// </summary>
public class ProjectExecutionSettings : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [MaxLength(500)]
    public string? MaiorRisco { get; set; }

    [Required]
    public ProjectExperienceType ExperienciaEquipe { get; set; }

    [Required]
    public DetailLevelType NivelDetalhePlano { get; set; }

    [Required]
    public ReviewFrequencyType FrequenciaRevisao { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [MaxLength(1000)]
    public string? OQueDeuCerto { get; set; }

    [MaxLength(1000)]
    public string? OQueDeuErrado { get; set; }

    public ProjectExecutionSettings() { }

    /// <summary>
    /// Atualiza as configurações de execução do projeto
    /// </summary>
    public void UpdateSettings(
        ProjectExperienceType experienciaEquipe,
        DetailLevelType nivelDetalhePlano,
        ReviewFrequencyType frequenciaRevisao,
        string? maiorRisco = null,
        string? observacoes = null,
        string? oQueDeuCerto = null,
        string? oQueDeuErrado = null)
    {
        ExperienciaEquipe = experienciaEquipe;
        NivelDetalhePlano = nivelDetalhePlano;
        FrequenciaRevisao = frequenciaRevisao;
        MaiorRisco = maiorRisco;
        Observacoes = observacoes;

        if (experienciaEquipe == ProjectExperienceType.NuncaFizemos)
        {
            OQueDeuCerto = null;
            OQueDeuErrado = null;
        }
        else
        {
            OQueDeuCerto = oQueDeuCerto;
            OQueDeuErrado = oQueDeuErrado;
        }

        SetUpdatedAt();
    }
}
