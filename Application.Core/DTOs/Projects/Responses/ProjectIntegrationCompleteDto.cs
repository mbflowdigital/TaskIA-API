namespace Application.Core.DTOs.Projects.Responses;

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
