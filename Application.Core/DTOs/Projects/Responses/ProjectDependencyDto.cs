namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de dependência externa do projeto
/// </summary>
public class ProjectDependencyDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Prazo { get; set; }
    public string Criticidade { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
