namespace Application.Core.DTOs.Companies;

/// <summary>
/// DTO de Empresa para retorno
/// </summary>
public record CompanyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public int NumberOfMembers { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int UserCount { get; init; }
}

/// <summary>
/// Request para criar empresa
/// </summary>
public record CreateCompanyRequest(
    string Name,
    string? Address,
    int NumberOfMembers,
    string? Category
);

/// <summary>
/// Request para atualizar empresa
/// </summary>
public record UpdateCompanyRequest(
    Guid Id,
    string Name,
    string? Address,
    int NumberOfMembers,
    string? Category
);
