namespace Application.Core.DTOs.Projects.Responses;

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
