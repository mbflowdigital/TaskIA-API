using Domain.Enums;

namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de integração do projeto
/// </summary>
public class ProjectIntegrationDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string NomeSistema { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public IntegrationStatusType Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
