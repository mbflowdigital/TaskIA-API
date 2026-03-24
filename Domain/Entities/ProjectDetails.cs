using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Entidade de Detalhes do Projeto
/// Armazena configurações operacionais e de conformidade do projeto
/// </summary>
public class ProjectDetails : BaseEntity
{
    // Relacionamento com Project
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    // Configurações Operacionais
    public bool TemDependenciasExternas { get; set; }

    public bool TemIntegracoes { get; set; }

    [Required]
    public BudgetType Orcamento { get; set; } = BudgetType.ADefinir;

    [Required]
    public WorkScheduleType HorarioTrabalho { get; set; } = WorkScheduleType.Comercial;

    [Required]
    public DowntimeType DowntimePermitido { get; set; } = DowntimeType.NaoSeAplica;

    // Campo condicional: obrigatório quando Orcamento = BudgetType.ValorFixo
    public decimal? ValorOrcamento { get; set; }

    // Campo condicional: obrigatório quando DowntimePermitido = DowntimeType.AteXHoras
    public int? HorasDowntime { get; set; }

    // Relacionamentos com Compliance e Períodos Indisponíveis
    public virtual ICollection<ProjectCompliance> Compliances { get; set; } = new List<ProjectCompliance>();

    public virtual ICollection<ProjectUnavailablePeriod> UnavailablePeriods { get; set; } = new List<ProjectUnavailablePeriod>();

    // Construtor público
    public ProjectDetails() { }

    /// <summary>
    /// Atualiza configurações operacionais do projeto
    /// </summary>
    public void UpdateOperationalSettings(
        bool temDependenciasExternas,
        bool temIntegracoes,
        BudgetType orcamento,
        WorkScheduleType horarioTrabalho,
        DowntimeType downtimePermitido,
        decimal? valorOrcamento = null,
        int? horasDowntime = null)
    {
        TemDependenciasExternas = temDependenciasExternas;
        TemIntegracoes = temIntegracoes;
        Orcamento = orcamento;
        HorarioTrabalho = horarioTrabalho;
        DowntimePermitido = downtimePermitido;
        ValorOrcamento = valorOrcamento;
        HorasDowntime = horasDowntime;
        SetUpdatedAt();
    }

    /// <summary>
    /// Adiciona um compliance ao projeto
    /// </summary>
    public void AddCompliance(ProjectCompliance compliance)
    {
        if (!Compliances.Any(c => c.TipoCompliance == compliance.TipoCompliance && c.IsActive))
        {
            Compliances.Add(compliance);
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Remove um compliance do projeto
    /// </summary>
    public void RemoveCompliance(Guid complianceId)
    {
        var compliance = Compliances.FirstOrDefault(c => c.Id == complianceId);
        if (compliance != null)
        {
            compliance.Deactivate();
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Adiciona um período indisponível ao projeto
    /// </summary>
    public void AddUnavailablePeriod(ProjectUnavailablePeriod period)
    {
        UnavailablePeriods.Add(period);
        SetUpdatedAt();
    }

    /// <summary>
    /// Remove um período indisponível do projeto
    /// </summary>
    public void RemoveUnavailablePeriod(Guid periodId)
    {
        var period = UnavailablePeriods.FirstOrDefault(p => p.Id == periodId);
        if (period != null)
        {
            period.Deactivate();
            SetUpdatedAt();
        }
    }
}
