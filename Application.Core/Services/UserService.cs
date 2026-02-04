using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Core.Services;

/// <summary>
/// Service de Usuários
/// Contém toda a lógica de negócio relacionada a usuários
/// Implementa IUserService seguindo Dependency Inversion Principle
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Cria um novo usuário
    /// TODO: Implementar validação de email duplicado, criação de usuário e persistência
    /// </summary>
    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar lógica de criação de usuário");
    }

    /// <summary>
    /// Busca usuário por ID
    /// TODO: Implementar busca e mapeamento para DTO
    /// </summary>
    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar busca de usuário por ID");
    }

    /// <summary>
    /// Lista todos os usuários ativos
    /// TODO: Implementar listagem e mapeamento
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar listagem de usuários");
    }

    /// <summary>
    /// Atualiza informações do usuário
    /// TODO: Implementar busca, atualização e persistência
    /// </summary>
    public async Task<Result<UserDto>> UpdateAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar atualização de usuário");
    }

    /// <summary>
    /// Desativa um usuário (soft delete)
    /// TODO: Implementar desativação
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar desativação de usuário");
    }

    /// <summary>
    /// Busca usuários por email
    /// TODO: Implementar busca por email
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar busca por email");
    }

    /// <summary>
    /// Verifica se email já está em uso
    /// TODO: Implementar verificação de email duplicado
    /// </summary>
    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Implementar verificação de email");
    }

    // TODO: Implementar método privado para mapear User -> UserDto
    // private static UserDto MapToDto(User user) { }
}
