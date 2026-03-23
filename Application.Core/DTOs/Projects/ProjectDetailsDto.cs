using Domain.Enums;

namespace Application.Core.DTOs.Projects;

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
    public WorkScheduleType HorarioTrabalho { get; set; }
    public string HorarioTrabalhoDescricao { get; set; } = string.Empty;
    public DowntimeType DowntimePermitido { get; set; }
    public string DowntimePermitidoDescricao { get; set; } = string.Empty;
    public List<ProjectComplianceDto> Compliances { get; set; } = new();
    public List<ProjectUnavailablePeriodDto> UnavailablePeriods { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para compliance do projeto
/// </summary>
public class ProjectComplianceDto
{
    public Guid Id { get; set; }
    public ComplianceType TipoCompliance { get; set; }
    public string TipoComplianceDescricao { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para período indisponível
/// </summary>
public class ProjectUnavailablePeriodDto
{
    public Guid Id { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool IsPeriodActive { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para criação de ProjectDetails
/// </summary>
public class CreateProjectDetailsRequest
{
    public Guid ProjectId { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public BudgetType Orcamento { get; set; } = BudgetType.ADefinir;
    public WorkScheduleType HorarioTrabalho { get; set; } = WorkScheduleType.Comercial;
    public DowntimeType DowntimePermitido { get; set; } = DowntimeType.NaoSeAplica;
    public List<CreateProjectComplianceRequest> Compliances { get; set; } = new();
    public List<CreateProjectUnavailablePeriodRequest> UnavailablePeriods { get; set; } = new();
}

/// <summary>
/// DTO para atualização de ProjectDetails
/// </summary>
public class UpdateProjectDetailsRequest
{
    public Guid Id { get; set; }
    public bool TemDependenciasExternas { get; set; }
    public bool TemIntegracoes { get; set; }
    public BudgetType Orcamento { get; set; }
    public WorkScheduleType HorarioTrabalho { get; set; }
    public DowntimeType DowntimePermitido { get; set; }
}

/// <summary>
/// DTO para adicionar compliance
/// </summary>
public class CreateProjectComplianceRequest
{
    public ComplianceType TipoCompliance { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para adicionar período indisponível
/// </summary>
public class CreateProjectUnavailablePeriodRequest
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
