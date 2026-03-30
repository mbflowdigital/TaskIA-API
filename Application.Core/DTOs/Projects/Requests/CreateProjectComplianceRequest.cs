using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para adicionar compliance ao projeto
/// </summary>
public class CreateProjectComplianceRequest
{
    public ComplianceType TipoCompliance { get; set; }
    public string? Observacoes { get; set; }
}
