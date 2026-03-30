using Domain.Enums;

namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de dado sensível do projeto
/// </summary>
public class ProjectSensitiveDataDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public SensitiveDataType TipoDadoSensivel { get; set; }
    public string TipoDadoSensivelDescricao { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
