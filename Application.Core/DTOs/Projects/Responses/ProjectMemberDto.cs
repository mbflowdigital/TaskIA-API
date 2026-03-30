namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para membros do projeto
/// </summary>
public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? ProjectFunction { get; set; }
    public string? Dedication { get; set; }
    public string? Approver { get; set; }
    public string? FunctionDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
