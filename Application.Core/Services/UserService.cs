using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Application.Core.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
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
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var anyUsers = await _userRepository.AnyAsync(cancellationToken);

            // Regras de permissão por perfil/logado
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

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
            var targetRole = UserRole.USER;
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                if (!Enum.TryParse<UserRole>(request.Role, true, out targetRole))
                {
                    return Result<UserDto>.Failure("Perfil inválido. Valores aceitos: ADM_MASTER, ADM, USER");
                }
            }

            if (actorRole == UserRole.USER)
            {
                return Result<UserDto>.Failure("Usuários padrão não podem criar outros usuários.");
            }

            // ADM só pode criar USER e sempre dentro da própria empresa
            if (actorRole == UserRole.ADM)
            {
                if (targetRole != UserRole.USER)
                    return Result<UserDto>.Failure("ADM só pode criar usuários do tipo USER.");
            }

            // ADM_MASTER pode criar ADM e USER (não pode criar ADM_MASTER)
            if (actorRole == UserRole.ADM_MASTER)
            {
                var canCreate = targetRole == UserRole.ADM || targetRole == UserRole.USER;
                if (!canCreate)
                    return Result<UserDto>.Failure("ADM_MASTER só pode criar ADM ou USER.");
            }

            // Regra de bootstrap: primeiro usuário do sistema vira ADM_MASTER
            if (!anyUsers)
            {
                targetRole = UserRole.ADM_MASTER;
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Phone = request.Phone,
                CPF = request.CPF.Replace(".", "").Replace("-", "").Trim(),
                BirthDate = request.BirthDate,
                Role = targetRole,
                CompanyId = actorRole == UserRole.ADM ? actor?.CompanyId : null
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
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {id}");
            }

            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result<UserDto>.Failure("Sem permissão para acessar usuário de outra empresa.");
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
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<UserDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            IEnumerable<User> users;
            if (actorRole == UserRole.ADM && actor?.CompanyId != null)
            {
                users = await _userRepository.GetByCompanyIdAsync(actor.CompanyId.Value, cancellationToken);
            }
            else
            {
                users = await _userRepository.GetAllAsync(cancellationToken);
            }

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
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {request.Id}");
            }

            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result<UserDto>.Failure("ADM não pode editar usuários de outra empresa.");
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
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result.Failure($"Usuário não encontrado com ID {id}");
            }

            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result.Failure("ADM não pode desativar usuários de outra empresa.");
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
            CompanyId = user.CompanyId,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CPF = user.CPF,
            BirthDate = user.BirthDate,
            Role = user.Role.ToString(),
            IsEmailVerified = user.IsEmailVerified,
            IsFirstAccess = user.IsFirstAccess,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private async Task<Result<User?>> ResolveActorAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken)
    {
        // Sem contexto de autenticação: mantém comportamento legado
        if (!actorRole.HasValue)
            return Result<User?>.Success(null);

        if (actorRole == UserRole.USER)
            return Result<User?>.Failure("Usuários padrão não possuem permissão para esta operação.");

        if (actorRole != UserRole.ADM)
            return Result<User?>.Success(null);

        if (!actorUserId.HasValue)
            return Result<User?>.Failure("Não foi possível identificar o usuário ADM logado.");

        var actor = await _userRepository.GetByIdAsync(actorUserId.Value, cancellationToken);
        if (actor == null || !actor.IsActive)
            return Result<User?>.Failure("Usuário ADM logado inválido ou inativo.");

        if (actor.CompanyId == null)
            return Result<User?>.Failure("ADM sem empresa vinculada não pode gerenciar equipe.");

        return Result<User?>.Success(actor);
    }
}
