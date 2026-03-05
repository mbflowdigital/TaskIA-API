using Application.Core.DTOs.Auth;
using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Application.Core.Services;

/// <summary>
/// Service de Autentica��o
/// Cont�m toda a l�gica de autentica��o de usu�rios
/// Senha padr�o: Data de nascimento no formato ddMMyyyy (ex: 25111998)
/// Preparado para evolu��o futura (JWT, refresh token, hash bcrypt, etc)
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Autentica usu�rio com CPF e senha
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Normalizar CPF (remover caracteres especiais)
            var cpf = NormalizeCPF(request.CPF);

            // 2. Buscar usu�rio por CPF
            var user = await _userRepository.GetByCPFAsync(cpf, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("CPF ou senha inv�lidos");
            }

            // 3. Validar se usu�rio est� ativo
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure("Usu�rio desativado. Entre em contato com o suporte.");
            }

            // 4. Validar senha
            var passwordHash = HashPassword(request.Password);
            if (user.PasswordHash != passwordHash)
            {
                return Result<LoginResponse>.Failure("CPF ou senha inv�lidos");
            }

            // 5. Criar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                IsFirstAccess = user.IsFirstAccess,
                RequiresOnboarding = user.Role == UserRole.ADM && user.CompanyId == null,
                
                // TODO: Futuro - Gerar JWT token
                Token = null,
                TokenExpiration = null
            };

            var message = user.IsFirstAccess 
                ? "Login realizado com sucesso. Por favor, altere sua senha." 
                : "Login realizado com sucesso";

            return Result<LoginResponse>.Success(response, message);
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<LoginResponse>.Failure($"Erro ao realizar login: {ex.Message}");
        }
    }

    /// <summary>
    /// Troca senha de primeiro acesso
    /// </summary>
    public async Task<Result<LoginResponse>> ChangePasswordFirstAccessAsync(
        ChangePasswordFirstAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Normalizar CPF
            var cpf = NormalizeCPF(request.CPF);

            // 2. Buscar usu�rio por CPF
            var user = await _userRepository.GetByCPFAsync(cpf, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("Usu�rio n�o encontrado");
            }

            // 3. Validar se usu�rio est� ativo
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure("Usu�rio desativado");
            }

            // 4. Validar senha atual
            var currentPasswordHash = HashPassword(request.CurrentPassword);
            if (user.PasswordHash != currentPasswordHash)
            {
                return Result<LoginResponse>.Failure("Senha atual inv�lida");
            }

            // 5. Validar se nova senha � diferente da atual
            var newPasswordHash = HashPassword(request.NewPassword);
            if (user.PasswordHash == newPasswordHash)
            {
                return Result<LoginResponse>.Failure("Nova senha n�o pode ser igual � senha atual");
            }

            // 6. Validar se nova senha n�o � a senha padr�o (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            var defaultPasswordHash = HashPassword(defaultPassword);
            if (newPasswordHash == defaultPasswordHash)
            {
                return Result<LoginResponse>.Failure($"Nova senha n�o pode ser a senha padr�o (data de nascimento: {defaultPassword})");
            }

            // 7. Atualizar senha (j� marca IsFirstAccess = false)
            user.SetPassword(newPasswordHash);

            // 8. Persistir altera��es
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 9. Retornar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                IsFirstAccess = user.IsFirstAccess, // Agora � false                RequiresOnboarding = user.Role == UserRole.ADM && user.CompanyId == null,                Token = null,
                TokenExpiration = null
            };

            return Result<LoginResponse>.Success(response, "Senha alterada com sucesso!");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<LoginResponse>.Failure($"Erro ao alterar senha: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica se CPF j� existe no sistema
    /// </summary>
    public async Task<bool> CPFExistsAsync(
        string cpf,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedCPF = NormalizeCPF(cpf);
            return await _userRepository.CPFExistsAsync(normalizedCPF, cancellationToken);
        }
        catch
        {
            // TODO: Implementar logging aqui
            return false;
        }
    }

    /// <summary>
    /// Normaliza CPF (remove caracteres especiais)
    /// </summary>
    private static string NormalizeCPF(string cpf)
    {
        return cpf.Replace(".", "").Replace("-", "").Replace(" ", "").Trim();
    }

    /// <summary>
    /// Hash simples da senha (SHA256)
    /// TODO: Futuro - Usar BCrypt para hash de senha
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Registra novo usuário validando permissão do criador (via role das Claims)
    /// </summary>
    public async Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        UserRole? createdByRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar permissão se JWT presente
            var requestedRole = UserRole.USER;
            if (!string.IsNullOrWhiteSpace(request.Role) &&
                Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsedRequested))
                requestedRole = parsedRequested;

            if (createdByRole.HasValue)
            {
                var canCreate = createdByRole.Value switch
                {
                    UserRole.ADM_MASTER => requestedRole == UserRole.ADM || requestedRole == UserRole.USER,
                    UserRole.ADM        => requestedRole == UserRole.USER,
                    _                   => false
                };
                if (!canCreate)
                    return Result<UserDto>.Failure("Sem permissão para criar usuário com este perfil.");
            }

            // 2. Normalizar e validar duplicatas
            var cpf = request.CPF.Replace(".", "").Replace("-", "").Trim();

            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
                return Result<UserDto>.Failure("Email já cadastrado.");

            if (await _userRepository.CPFExistsAsync(cpf, cancellationToken))
                return Result<UserDto>.Failure("CPF já cadastrado.");

            // 3. Primeiro usuário sem JWT → ADM_MASTER
            if (!createdByRole.HasValue)
            {
                var all = await _userRepository.GetAllAsync(cancellationToken);
                if (!all.Any())
                    requestedRole = UserRole.ADM_MASTER;
            }

            // 4. Criar entidade
            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Phone = request.Phone,
                CPF = cpf,
                BirthDate = request.BirthDate,
                Role = requestedRole
            };

            var defaultPassword = user.GetDefaultPassword();
            user.PasswordHash = HashPassword(defaultPassword);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = new UserDto
            {
                Id = user.Id,
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

            return Result<UserDto>.Success(dto,
                $"Usuário registrado com sucesso. Senha padrão: {defaultPassword}");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure($"Erro ao registrar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Completa onboarding do ADM: cria empresa e vincula ao usuário.
    /// </summary>
    public async Task<Result<LoginResponse>> OnboardingAsync(
        OnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar usuário
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<LoginResponse>.Failure("Usuário não encontrado.");

            // 2. Validar que é ADM sem empresa
            if (user.Role != UserRole.ADM)
                return Result<LoginResponse>.Failure("Apenas usuários ADM precisam completar o onboarding.");

            if (user.CompanyId != null)
                return Result<LoginResponse>.Failure("Usuário já possui empresa vinculada.");

            // 3. Criar empresa
            var company = new Domain.Entities.Company
            {
                Name = request.CompanyName.Trim(),
                Address = request.Address.Trim(),
                NumberOfMembers = request.NumberOfMembers,
                Category = request.Category.Trim()
            };

            await _companyRepository.AddAsync(company, cancellationToken);

            // 4. Vincular usuário à empresa
            user.CompanyId = company.Id;
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            // 5. Retornar response atualizado
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                IsFirstAccess = user.IsFirstAccess,
                RequiresOnboarding = false,
                Token = null,
                TokenExpiration = null
            };

            return Result<LoginResponse>.Success(response, "Onboarding concluído com sucesso!");
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Failure($"Erro ao concluir onboarding: {ex.Message}");
        }
    }

    // TODO: Métodos futuros
    // public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    // public async Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    // public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
}
