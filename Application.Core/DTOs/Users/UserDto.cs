namespace Application.Core.DTOs.Users;

/// <summary>
/// DTO de Usuário para retorno
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public int PositionId { get; init; }
    public string? PositionName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string CPF { get; init; } = string.Empty;
    public DateTime BirthDate { get; init; }
    public string Role { get; init; } = "USER";
    public bool IsEmailVerified { get; init; }
    public bool IsFirstAccess { get; init; }
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
    string CPF,
    DateTime BirthDate,
    string? Phone = null,
    string? Role = null,
    Guid? CompanyId = null,
    int? PositionId = null
);

/// <summary>
/// Request para atualizar usuário
/// </summary>
public record UpdateUserRequest(
    Guid Id,
    string Name,
    string? Phone = null,
    string? CPF = null,
    DateTime? BirthDate = null,
    Guid? CompanyId = null,
    int? PositionId = null
)
{
};

public record ViaCEp(
    string Cep,
    string Logradouro,
    string Bairro,
    string Complemento,
    string unidade,
    string localidade,
    string uf,
    string estado,
    string regioao,
    string ibge,
    string gia,
    string ddd,
    string siafi)
{};

/// <summary>
/// Request para buscar usuário por ID
/// </summary>
public record GetUserByIdRequest(Guid Id);

/// <summary>
/// Request para listar todos os usuários
/// </summary>
public record GetAllUsersRequest();
