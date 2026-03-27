namespace Application.Core.DTOs.Projects.Responses;

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
