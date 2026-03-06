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
/// Service de Autenticação
/// Contém toda a lógica de autenticação de usuários
/// Senha padrão: Data de nascimento no formato ddMMyyyy (ex: 25111998)
/// 
/// MIGRAÇÃO HÍBRIDA SHA256 → BCrypt:
/// - Detecta automaticamente o formato do hash
/// - Migra para BCrypt no próximo login bem-sucedido
/// - Não requer reset de senhas dos usuários
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ICompanyRepository companyRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }



    /// <summary>
    /// Autentica usuário com CPF e senha
    /// Migra automaticamente SHA256 → BCrypt no login bem-sucedido
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

            // 4. Validar senha (suporta SHA256 legado e BCrypt)
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure("CPF ou senha inv�lidos");
            }

            // 5. 🔄 MIGRAÇÃO AUTOMÁTICA: Se hash é SHA256, converter para BCrypt
            var needsMigration = !_passwordHasher.IsBcryptHash(user.PasswordHash);
            if (needsMigration)
            {
                var newPasswordHash = _passwordHasher.HashPassword(request.Password);
                user.PasswordHash = newPasswordHash;

                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            // 6. Gerar JWT Token e Refresh Token
            var token = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Name,
                user.CPF);

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var tokenExpiration = _jwtTokenService.GetTokenExpirationDate(token) ?? DateTime.UtcNow.AddHours(1);

            // 7. Criar response
            // 5. Criar response
            var companyName = user.CompanyId.HasValue
                ? (await _companyRepository.GetByIdAsync(user.CompanyId.Value, cancellationToken))?.Name
                : null;

            var response = new LoginResponse
            {
                UserId = user.Id,
                CompanyId = user.CompanyId,
                CompanyName = companyName,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                IsFirstAccess = user.IsFirstAccess,
                Token = token,
                TokenExpiration = tokenExpiration,
                RefreshToken = refreshToken
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
    /// Realiza logout do usuário revogando o token JWT
    /// Usa JwtTokenService para gerenciar a blacklist
    /// </summary>
    public async Task<Result> LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure("Token não fornecido");
            }

            // Revogar token usando JwtTokenService
            await _jwtTokenService.RevokeTokenAsync(token);

            return Result.Success("Logout realizado com sucesso");
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao realizar logout: {ex.Message}");
        }
    }

    /// <summary>
    /// Troca senha de primeiro acesso
    /// Nova senha sempre será BCrypt
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

            // 4. Validar senha atual (suporta SHA256 e BCrypt)
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure("Senha atual inv�lida");
            }

            // 5. Validar se nova senha é diferente da atual
            if (request.NewPassword == request.CurrentPassword)
            {
                return Result<LoginResponse>.Failure("Nova senha n�o pode ser igual � senha atual");
            }

            // 6. Validar se nova senha n�o � a senha padr�o (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            if (request.NewPassword == defaultPassword)
            {
                return Result<LoginResponse>.Failure($"Nova senha n�o pode ser a senha padr�o (data de nascimento: {defaultPassword})");
            }

            // 7. Gerar hash BCrypt da nova senha (sempre BCrypt para novas senhas)
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.SetPassword(newPasswordHash);

            // 8. Atualizar senha (já marca IsFirstAccess = false)
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 9. Gerar JWT Token e Refresh Token
            var token = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Name,
                user.CPF);

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var tokenExpiration = _jwtTokenService.GetTokenExpirationDate(token) ?? DateTime.UtcNow.AddHours(1);

            var companyName = user.CompanyId.HasValue
                ? (await _companyRepository.GetByIdAsync(user.CompanyId.Value, cancellationToken))?.Name
                : null;
            // 10. Retornar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                CompanyId = user.CompanyId,
                CompanyName = companyName,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                IsFirstAccess = user.IsFirstAccess,
                Token = token,
                TokenExpiration = tokenExpiration,
                RefreshToken = refreshToken,
                 Role = user.Role.ToString(),
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
    /// Renova token JWT usando refresh token
    /// Valida o token expirado e gera novos tokens (access + refresh)
    /// </summary>
    public async Task<Result<LoginResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar entrada
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<LoginResponse>.Failure("Token e Refresh Token são obrigatórios");
            }

            // 2. Extrair claims do token expirado
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return Result<LoginResponse>.Failure("Token inválido");
            }

            // 3. Extrair UserId das claims
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Result<LoginResponse>.Failure("Token inválido: UserId não encontrado");
            }

            // 4. Buscar usuário
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("Usuário não encontrado");
            }

            // 5. Validar se usuário está ativo
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure("Usuário desativado");
            }

            // 6. TODO: Validar refresh token no banco (quando implementarmos persistência)
            // Por ora, validamos apenas se foi fornecido

            // 7. Gerar novos tokens
            var newToken = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Name,
                user.CPF);

            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var tokenExpiration = _jwtTokenService.GetTokenExpirationDate(newToken) ?? DateTime.UtcNow.AddHours(1);

            // 8. Criar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                IsFirstAccess = user.IsFirstAccess,
                Token = newToken,
                TokenExpiration = tokenExpiration,
                RefreshToken = newRefreshToken
            };

            return Result<LoginResponse>.Success(response, "Token renovado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<LoginResponse>.Failure($"Erro ao renovar token: {ex.Message}");
        }
    }

    /// <summary>
    /// Troca senha do usuário autenticado
    /// Valida senha atual e atualiza para nova senha com BCrypt
    /// </summary>
    public async Task<Result> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar usuário
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("Usuário não encontrado");
            }

            // 2. Validar se usuário está ativo
            if (!user.IsActive)
            {
                return Result.Failure("Usuário desativado");
            }

            // 3. Validar senha atual
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure("Senha atual inválida");
            }

            // 4. Validar se nova senha é diferente da atual
            if (request.NewPassword == request.CurrentPassword)
            {
                return Result.Failure("Nova senha não pode ser igual à senha atual");
            }

            // 5. Validar se as senhas coincidem
            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result.Failure("Nova senha e confirmação não coincidem");
            }

            // 6. Validar força da senha (mínimo 8 caracteres)
            if (request.NewPassword.Length < 8)
            {
                return Result.Failure("Nova senha deve ter no mínimo 8 caracteres");
            }

            // 7. Validar se nova senha não é a senha padrão (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            if (request.NewPassword == defaultPassword)
            {
                return Result.Failure($"Nova senha não pode ser a senha padrão (data de nascimento: {defaultPassword})");
            }

            // 8. Gerar hash BCrypt da nova senha
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.SetPassword(newPasswordHash);

            // 9. Atualizar usuário
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Senha alterada com sucesso!");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao alterar senha: {ex.Message}");
        }
    }

    /// <summary>
    /// Reseta senha usando CPF (esqueceu a senha)
    /// Aplica mesmas validações de troca de senha
    /// </summary>
    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar entrada
            if (string.IsNullOrWhiteSpace(request.CPF))
            {
                return Result.Failure("CPF é obrigatório");
            }

            // 2. Normalizar CPF e buscar usuário
            var cpf = NormalizeCPF(request.CPF);
            var user = await _userRepository.GetByCPFAsync(cpf, cancellationToken);
            
            if (user == null)
            {
                return Result.Failure("CPF não encontrado");
            }

            // 3. Validar se usuário está ativo
            if (!user.IsActive)
            {
                return Result.Failure("Usuário desativado");
            }

            // 4. Validar se as senhas coincidem
            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result.Failure("Nova senha e confirmação não coincidem");
            }

            // 5. Validar força da senha (mínimo 8 caracteres)
            if (request.NewPassword.Length < 8)
            {
                return Result.Failure("Nova senha deve ter no mínimo 8 caracteres");
            }

            // 6. Validar se nova senha não é a senha padrão (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            if (request.NewPassword == defaultPassword)
            {
                return Result.Failure($"Nova senha não pode ser a senha padrão (data de nascimento: {defaultPassword})");
            }

            // 7. Validar se nova senha é diferente da senha atual
            if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                return Result.Failure("Nova senha não pode ser igual à senha atual");
            }

            // 8. Gerar hash BCrypt da nova senha
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.SetPassword(newPasswordHash);

            // 9. Atualizar usuário
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Senha redefinida com sucesso! Faça login com sua nova senha.");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao redefinir senha: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica se CPF já existe no sistema
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
                CompanyId = user.CompanyId,
                CompanyName = company.Name,
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
