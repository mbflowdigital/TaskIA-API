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
    /// EXEMPLO: Padrão de implementação com validação, persistência e tratamento de erros
    /// </summary>
    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar se email já existe
            var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
            if (emailExists)
            {
                return Result<UserDto>.Failure(
                    "Email já cadastrado. Este email já está sendo utilizado por outro usuário.");
            }

            // 2. Criar a entidade (regras de negócio na entidade)
            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Phone = request.Phone,
                IsEmailVerified = false
            };

            // 3. Adicionar ao repositório
            await _userRepository.AddAsync(user, cancellationToken);

            // 4. Salvar alterações (Unit of Work)
            await _unitOfWork.CommitAsync(cancellationToken);

            // 5. Mapear para DTO e retornar
            var userDto = MapToDto(user);
            return Result<UserDto>.Success(userDto, "Usuário criado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao criar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca usuário por ID
    /// EXEMPLO: Padrão de busca com validação de existência
    /// </summary>
    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // 1. Buscar no repositório
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        // 2. Validar se encontrou
        if (user == null)
        {
            return Result<UserDto>.Failure(
                $"Usuário não encontrado. Não foi encontrado usuário com ID {id}");
        }

        // 3. Mapear para DTO e retornar
        var userDto = MapToDto(user);
        return Result<UserDto>.Success(userDto);
    }

    /// <summary>
    /// Lista todos os usuários ativos
    /// EXEMPLO: Padrão de listagem com mapeamento em lote
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Buscar todos no repositório
        var users = await _userRepository.GetAllAsync(cancellationToken);

        // 2. Mapear para lista de DTOs
        var userDtos = users.Select(MapToDto).ToList();

        // 3. Retornar com mensagem de sucesso
        return Result<IEnumerable<UserDto>>.Success(
            userDtos,
            $"{userDtos.Count} usuário(s) encontrado(s)");
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

    /// <summary>
    /// Mapeia entidade User para UserDto
    /// EXEMPLO: Padrão de mapeamento manual (pode usar AutoMapper se preferir)
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            IsEmailVerified = user.IsEmailVerified,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
