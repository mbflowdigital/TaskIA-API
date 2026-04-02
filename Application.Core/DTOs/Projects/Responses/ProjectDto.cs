namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de dados do Project
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ResponsibleSector { get; set; }
    public string? ProjectType { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TaskCount { get; set; }
    public List<ProjectMemberDto> Members { get; set; } = new();
    public ProjectDetailsDto? Details { get; set; }
    public ProjectExecutionSettingsDto? ExecutionSettings { get; set; }
}
