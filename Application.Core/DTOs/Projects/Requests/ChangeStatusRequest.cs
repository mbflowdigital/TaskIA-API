namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para alteração de status do projeto (Active/Inactive)
/// </summary>
public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
