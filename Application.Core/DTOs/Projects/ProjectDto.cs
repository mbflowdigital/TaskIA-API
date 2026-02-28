namespace Application.Core.DTOs.Projects;

/// <summary>
/// DTO para retorno de dados do Project
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; } // Nome do usuário que criou
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

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
    public Guid UserId { get; set; } // Obrigatório: quem está criando
}

/// <summary>
/// DTO para atualização de Project
/// </summary>
public class UpdateProjectRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    // UserId não pode ser alterado (quem criou permanece)
}
