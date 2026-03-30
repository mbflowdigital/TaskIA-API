namespace Application.Core.DTOs.Projects.Responses;

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
