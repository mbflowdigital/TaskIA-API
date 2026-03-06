using Application.Core.DTOs.Users;
using Domain.Common;
using Domain.Enums;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do serviço de Usuários
/// Seguindo Dependency Inversion Principle - Dependência de abstração, não de implementação
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os usuários ativos
    /// </summary>
    Task<Result<IEnumerable<UserDto>>> GetAllAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza informações do usuário
    /// </summary>
    Task<Result<UserDto>> UpdateAsync(
        UpdateUserRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Desativa um usuário (soft delete)
    /// </summary>
    Task<Result> DeleteAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuários por email
    /// </summary>
    Task<Result<IEnumerable<UserDto>>> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se email já está em uso
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
