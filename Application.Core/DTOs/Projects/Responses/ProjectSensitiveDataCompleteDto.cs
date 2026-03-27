namespace Application.Core.DTOs.Projects.Responses;

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
