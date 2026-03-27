using Domain.Enums;

namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de ProjectDetails
/// </summary>
public class ProjectDetailsDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public BudgetType Orcamento { get; set; }
    public string OrcamentoDescricao { get; set; } = string.Empty;
    public decimal? ValorOrcamento { get; set; }
    public WorkScheduleType HorarioTrabalho { get; set; }
    public string HorarioTrabalhoDescricao { get; set; } = string.Empty;
    public DowntimeType DowntimePermitido { get; set; }
    public string DowntimePermitidoDescricao { get; set; } = string.Empty;
    public int? HorasDowntime { get; set; }
    public List<ProjectComplianceDto> Compliances { get; set; } = new();
    public List<ProjectUnavailablePeriodDto> UnavailablePeriods { get; set; } = new();
    public List<ProjectDependencyDto> Dependencies { get; set; } = new();
    public List<ProjectIntegrationDto> Integrations { get; set; } = new();
    public List<ProjectSensitiveDataDto> SensitiveData { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
