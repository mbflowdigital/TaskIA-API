namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// Visão completa do projeto com todas as seções ativas e todos os campos enum convertidos para texto
/// </summary>
public class ProjectCompleteDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ResponsibleSector { get; set; }
    public string? ProjectType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProjectMemberCompleteDto> Members { get; set; } = new();
    public ProjectDetailsCompleteDto? Details { get; set; }
    public ProjectExecutionSettingsCompleteDto? ExecutionSettings { get; set; }
}
