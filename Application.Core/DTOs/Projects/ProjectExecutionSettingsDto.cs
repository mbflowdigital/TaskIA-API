using Domain.Enums;

namespace Application.Core.DTOs.Projects;

/// <summary>
/// DTO para retorno de ProjectExecutionSettings com as prioridades ordenadas e os nomes reais dos enums
/// </summary>
public class ProjectExecutionSettingsDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public ProjectExperienceType ExperienciaEquipe { get; set; }
    public string ExperienciaEquipeDescricao { get; set; } = string.Empty;
    public DetailLevelType NivelDetalhePlano { get; set; }
    public string NivelDetalhePlanoDescricao { get; set; } = string.Empty;
    public ReviewFrequencyType FrequenciaRevisao { get; set; }
    public string FrequenciaRevisaoDescricao { get; set; } = string.Empty;
    public string? MaiorRisco { get; set; }
    public string? Observacoes { get; set; }
    public string? OQueDeuCerto { get; set; }
    public string? OQueDeuErrado { get; set; }
    public List<ProjectPriorityRankingDto> PrioridadesOrdenadas { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para retorno de uma prioridade do projeto com o nome real do enum
/// </summary>
public class ProjectPriorityRankingDto
{
    public Guid Id { get; set; }
    public PriorityType PriorityType { get; set; }
    public string PriorityTypeDescricao { get; set; } = string.Empty;
    public int Posicao { get; set; }
}

/// <summary>
/// DTO para criação de ProjectExecutionSettings
/// </summary>
public class CreateProjectExecutionSettingsRequest
{
    public ProjectExperienceType ExperienciaEquipe { get; set; }
    public DetailLevelType NivelDetalhePlano { get; set; }
    public ReviewFrequencyType FrequenciaRevisao { get; set; }
    public string? MaiorRisco { get; set; }
    public string? Observacoes { get; set; }
    public string? OQueDeuCerto { get; set; }
    public string? OQueDeuErrado { get; set; }
    public List<CreateProjectPriorityRankingRequest> PrioridadesOrdenadas { get; set; } = new();
}

/// <summary>
/// DTO para atualização de ProjectExecutionSettings
/// </summary>
public class UpdateProjectExecutionSettingsRequest
{
    public ProjectExperienceType ExperienciaEquipe { get; set; }
    public DetailLevelType NivelDetalhePlano { get; set; }
    public ReviewFrequencyType FrequenciaRevisao { get; set; }
    public string? MaiorRisco { get; set; }
    public string? Observacoes { get; set; }
    public string? OQueDeuCerto { get; set; }
    public string? OQueDeuErrado { get; set; }
    public List<CreateProjectPriorityRankingRequest> PrioridadesOrdenadas { get; set; } = new();
}

/// <summary>
/// DTO para criação de uma entrada no ranking de prioridades do projeto
/// </summary>
public class CreateProjectPriorityRankingRequest
{
    public PriorityType PriorityType { get; set; }
    public int Posicao { get; set; }
}
