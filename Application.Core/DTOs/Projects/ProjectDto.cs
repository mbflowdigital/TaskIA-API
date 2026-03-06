namespace Application.Core.DTOs.Projects;

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
    public Guid UserId { get; set; }
    public string? UserName { get; set; } // Nome do usu�rio que criou
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para cria��o de novo Project
/// </summary>
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid UserId { get; set; } // Obrigat�rio: quem est� criando
}

/// <summary>
/// DTO para atualiza��o de Project
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
    // UserId n�o pode ser alterado (quem criou permanece)
}

/// <summary>
/// DTO para altera��o de status do projeto (Active/Inactive)
/// </summary>
public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty; // "Active" ou "Inactive"
}
