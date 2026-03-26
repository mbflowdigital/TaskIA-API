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

/// <summary>
/// DTO para retorno de dependência externa do projeto
/// </summary>
public class ProjectDependencyDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Prazo { get; set; }
    public string Criticidade { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de dependência externa do projeto
/// </summary>
public class CreateProjectDependencyRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Prazo { get; set; }
    public string Criticidade { get; set; } = string.Empty;
}

/// <summary>
/// DTO para retorno de integração do projeto
/// </summary>
public class ProjectIntegrationDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string NomeSistema { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public IntegrationStatusType Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de integração do projeto
/// </summary>
public class CreateProjectIntegrationRequest
{
    public string NomeSistema { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public IntegrationStatusType Status { get; set; } = IntegrationStatusType.Existe;
}

/// <summary>
/// DTO para retorno de dado sensível do projeto
/// </summary>
public class ProjectSensitiveDataDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public SensitiveDataType TipoDadoSensivel { get; set; }
    public string TipoDadoSensivelDescricao { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para criação de dado sensível do projeto
/// </summary>
public class CreateProjectSensitiveDataRequest
{
    public SensitiveDataType TipoDadoSensivel { get; set; }
}
