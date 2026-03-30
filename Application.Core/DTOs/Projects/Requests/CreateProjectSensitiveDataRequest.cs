using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de dado sensível do projeto
/// </summary>
public class CreateProjectSensitiveDataRequest
{
    public SensitiveDataType TipoDadoSensivel { get; set; }
}
