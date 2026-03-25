namespace Application.Core.DTOs.Projects;

/// <summary>
/// Visão completa do projeto com todas as seções ativas e todos os campos enum convertidos para texto
/// </summary>
public class ProjectCompleteDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ResponsibleSector { get; set; }
    public string? ProjectType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ProjectMemberCompleteDto> Members { get; set; } = new();
    public ProjectDetailsCompleteDto? Details { get; set; }
    public ProjectExecutionSettingsCompleteDto? ExecutionSettings { get; set; }
}

/// <summary>
/// Membro do projeto na visão completa
/// </summary>
public class ProjectMemberCompleteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? ProjectFunction { get; set; }
    public string? Dedication { get; set; }
    public string? Approver { get; set; }
    public string? FunctionDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

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

/// <summary>
/// Compliance na visão completa — TipoCompliance como texto
/// </summary>
public class ProjectComplianceCompleteDto
{
    public Guid Id { get; set; }
    public string TipoCompliance { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Período indisponível na visão completa
/// </summary>
public class ProjectUnavailablePeriodCompleteDto
{
    public Guid Id { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Dependência externa na visão completa
/// </summary>
public class ProjectDependencyCompleteDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Prazo { get; set; }
    public string Criticidade { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Integração na visão completa — Status como texto
/// </summary>
public class ProjectIntegrationCompleteDto
{
    public Guid Id { get; set; }
    public string NomeSistema { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Dado sensível na visão completa — TipoDadoSensivel como texto
/// </summary>
public class ProjectSensitiveDataCompleteDto
{
    public Guid Id { get; set; }
    public string TipoDadoSensivel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Configurações de execução na visão completa — todos os enums como texto, prioridades ordenadas por posição
/// </summary>
public class ProjectExecutionSettingsCompleteDto
{
    public Guid Id { get; set; }
    public string ExperienciaEquipe { get; set; } = string.Empty;
    public string NivelDetalhePlano { get; set; } = string.Empty;
    public string FrequenciaRevisao { get; set; } = string.Empty;
    public string? MaiorRisco { get; set; }
    public string? Observacoes { get; set; }
    public string? OQueDeuCerto { get; set; }
    public string? OQueDeuErrado { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProjectPriorityRankingCompleteDto> PrioridadesOrdenadas { get; set; } = new();
}

/// <summary>
/// Prioridade do projeto na visão completa — PriorityType como texto, ordenado por Posicao
/// </summary>
public class ProjectPriorityRankingCompleteDto
{
    public Guid Id { get; set; }
    public int Posicao { get; set; }
    public string PriorityType { get; set; } = string.Empty;
}
