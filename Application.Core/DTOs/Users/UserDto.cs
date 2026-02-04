namespace Application.Core.DTOs.Users;

/// <summary>
/// DTO de Usuário para retorno
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Request para criar usuário
/// </summary>
public record CreateUserRequest(
    string Name,
    string Email,
    string? Phone = null
);

/// <summary>
/// Request para atualizar usuário
/// </summary>
public record UpdateUserRequest(
    Guid Id,
    string Name,
    string? Phone = null
);

/// <summary>
/// Request para buscar usuário por ID
/// </summary>
public record GetUserByIdRequest(Guid Id);

/// <summary>
/// Request para listar todos os usuários
/// </summary>
public record GetAllUsersRequest();
