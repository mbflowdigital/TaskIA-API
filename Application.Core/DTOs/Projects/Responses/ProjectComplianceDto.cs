using Domain.Enums;

namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para compliance do projeto
/// </summary>
public class ProjectComplianceDto
{
    public Guid Id { get; set; }
    public ComplianceType TipoCompliance { get; set; }
    public string TipoComplianceDescricao { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
