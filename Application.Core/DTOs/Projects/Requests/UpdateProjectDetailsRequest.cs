using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para atualização de ProjectDetails
/// </summary>
public class UpdateProjectDetailsRequest
{
    public Guid Id { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public BudgetType Orcamento { get; set; }
    public decimal? ValorOrcamento { get; set; }
    public WorkScheduleType HorarioTrabalho { get; set; }
    public DowntimeType DowntimePermitido { get; set; }
    public int? HorasDowntime { get; set; }
    public List<CreateProjectDependencyRequest> Dependencies { get; set; } = new();
    public List<CreateProjectIntegrationRequest> Integrations { get; set; } = new();
    public List<CreateProjectSensitiveDataRequest> SensitiveData { get; set; } = new();
}
