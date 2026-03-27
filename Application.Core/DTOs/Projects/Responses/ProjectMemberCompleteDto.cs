namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// Membro do projeto na visão completa
/// </summary>
public class ProjectMemberCompleteDto
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
