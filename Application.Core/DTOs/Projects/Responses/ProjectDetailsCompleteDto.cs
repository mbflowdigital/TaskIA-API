namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// Detalhes operacionais na visão completa — enums retornados somente como texto
/// </summary>
public class ProjectDetailsCompleteDto
{
    public Guid Id { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public string Orcamento { get; set; } = string.Empty;
    public decimal? ValorOrcamento { get; set; }
    public string HorarioTrabalho { get; set; } = string.Empty;
    public string DowntimePermitido { get; set; } = string.Empty;
    public int? HorasDowntime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProjectComplianceCompleteDto> Compliances { get; set; } = new();
    public List<ProjectUnavailablePeriodCompleteDto> UnavailablePeriods { get; set; } = new();
    public List<ProjectDependencyCompleteDto> Dependencies { get; set; } = new();
    public List<ProjectIntegrationCompleteDto> Integrations { get; set; } = new();
    public List<ProjectSensitiveDataCompleteDto> SensitiveData { get; set; } = new();
}
