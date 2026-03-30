namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para adicionar membro ao projeto
/// </summary>
public class CreateProjectMemberRequest
{
    public Guid UserId { get; set; }
    public string? ProjectFunction { get; set; }
    public string? Dedication { get; set; }
    public string? Approver { get; set; }
    public string? FunctionDescription { get; set; }
}
