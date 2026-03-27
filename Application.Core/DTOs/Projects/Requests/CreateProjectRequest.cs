namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de novo Project
/// </summary>
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ResponsibleSector { get; set; }
    public string? ProjectType { get; set; }
    public Guid UserId { get; set; }
    public List<CreateProjectMemberRequest> Members { get; set; } = new();
}
