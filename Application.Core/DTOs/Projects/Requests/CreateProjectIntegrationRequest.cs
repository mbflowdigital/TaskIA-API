using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de integração do projeto
/// </summary>
public class CreateProjectIntegrationRequest
{
    public string NomeSistema { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Criticidade { get; set; } = string.Empty;
    public IntegrationStatusType Status { get; set; } = IntegrationStatusType.Existe;
}
