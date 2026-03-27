using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

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
