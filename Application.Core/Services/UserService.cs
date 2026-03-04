using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Application.Core.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Core.Services;

/// <summary>
/// Service de Users
/// Contém toda a lógica de negócio relacionada a usuários
/// Implementa IUserService seguindo Dependency Inversion Principle
/// Este é um EXEMPLO de Service para os desenvolvedores seguirem o padrão
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
                    "Email já cadastrado. Escolha outro email para o usuário.");
            }

            // 2. Validar se CPF já existe
            var cpfExists = await _userRepository.CPFExistsAsync(request.CPF, cancellationToken);
            if (cpfExists)
            {
                return Result<UserDto>.Failure(
                    "CPF já cadastrado. Escolha outro CPF.");
            }

            // 3. Criar entidade User
            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Phone = request.Phone,
                CPF = request.CPF.Replace(".", "").Replace("-", "").Trim(),
                BirthDate = request.BirthDate
            };

            // 4. Hash da senha padrão (data de nascimento: ddMMyyyy)
            var defaultPassword = user.GetDefaultPassword();
            user.PasswordHash = AuthService.HashPassword(defaultPassword);

            // 5. Adicionar ao repositório
            await _userRepository.AddAsync(user, cancellationToken);

            // 6. Salvar alterações
            await _unitOfWork.CommitAsync(cancellationToken);

            // 7. Retornar DTO
            return Result<UserDto>.Success(
                MapToDto(user), 
                $"Usuário criado com sucesso. Senha padrão: {defaultPassword} (alterar no primeiro acesso)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao criar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {id}");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao buscar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista todos os usuários ativos
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            var userDtos = users.Where(u => u.IsActive).Select(MapToDto).ToList();
            
            return Result<IEnumerable<UserDto>>.Success(
                userDtos,
                $"{userDtos.Count} usuário(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<UserDto>>.Failure($"Erro ao listar usuários: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza informações do usuário
    /// </summary>
    public async Task<Result<UserDto>> UpdateAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {request.Id}");
            }

            if (!user.IsActive)
            {
                return Result<UserDto>.Failure("Usuário está desativado e não pode ser atualizado");
            }

            // Atualizar informações
            user.UpdateProfile(request.Name, request.Phone);

            // Persistir alterações
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<UserDto>.Success(MapToDto(user), "Usuário atualizado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao atualizar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Desativa um usuário (soft delete)
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result.Failure($"Usuário não encontrado com ID {id}");
            }

            if (!user.IsActive)
            {
                return Result.Success("Usuário já está desativado");
            }

            // Soft delete
            user.SoftDelete();

            // Persistir alterações
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Usuário desativado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao desativar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca usuários por email
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                return Result<IEnumerable<UserDto>>.Success(
                    Enumerable.Empty<UserDto>(),
                    "Nenhum usuário encontrado");
            }

            var users = new List<UserDto> { MapToDto(user) };
            return Result<IEnumerable<UserDto>>.Success(users, "Usuário encontrado");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<UserDto>>.Failure($"Erro ao buscar usuário por email: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica se email já está em uso
    /// </summary>
    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userRepository.EmailExistsAsync(email, cancellationToken);
        }
        catch
        {
            // TODO: Implementar logging aqui
            return false;
        }
    }

    /// <summary>
    /// Mapeia entidade User para UserDto
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CPF = user.CPF,
            BirthDate = user.BirthDate,
            IsEmailVerified = user.IsEmailVerified,
            IsFirstAccess = user.IsFirstAccess,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
