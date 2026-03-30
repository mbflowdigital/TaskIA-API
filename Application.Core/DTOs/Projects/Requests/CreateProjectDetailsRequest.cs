using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de ProjectDetails
/// </summary>
public class CreateProjectDetailsRequest
{
    public Guid ProjectId { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public BudgetType Orcamento { get; set; } = BudgetType.ADefinir;
    public decimal? ValorOrcamento { get; set; }
    public WorkScheduleType HorarioTrabalho { get; set; } = WorkScheduleType.Comercial;
    public DowntimeType DowntimePermitido { get; set; } = DowntimeType.NaoSeAplica;
    public int? HorasDowntime { get; set; }
    public List<CreateProjectComplianceRequest> Compliances { get; set; } = new();
    public List<CreateProjectUnavailablePeriodRequest> UnavailablePeriods { get; set; } = new();
    public List<CreateProjectDependencyRequest> Dependencies { get; set; } = new();
    public List<CreateProjectIntegrationRequest> Integrations { get; set; } = new();
    public List<CreateProjectSensitiveDataRequest> SensitiveData { get; set; } = new();
}
