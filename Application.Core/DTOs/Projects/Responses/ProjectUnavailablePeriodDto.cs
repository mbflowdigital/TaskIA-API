namespace Application.Core.DTOs.Projects.Responses;

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
