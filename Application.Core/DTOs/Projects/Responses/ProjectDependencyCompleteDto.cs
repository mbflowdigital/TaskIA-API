namespace Application.Core.DTOs.Projects.Responses;

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
