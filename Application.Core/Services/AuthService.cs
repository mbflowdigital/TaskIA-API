using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Interfaces;

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
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

            // 2. Buscar usuário por CPF
            var user = await _userRepository.GetByCPFAsync(cpf, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("CPF ou senha inválidos");
            }

            // 3. Validar se usuário está ativo
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure("Usuário desativado. Entre em contato com o suporte.");
            }

            // 4. Validar senha (suporta SHA256 legado e BCrypt)
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure("CPF ou senha inválidos");
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

            // 6. Gerar JWT Token
            var token = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Name,
                user.CPF);

            var tokenExpiration = _jwtTokenService.GetTokenExpirationDate(token) ?? DateTime.UtcNow.AddHours(1);

            // 7. Criar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                IsFirstAccess = user.IsFirstAccess,
                Token = token,
                TokenExpiration = tokenExpiration
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

            // 2. Buscar usuário por CPF
            var user = await _userRepository.GetByCPFAsync(cpf, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("Usuário não encontrado");
            }

            // 3. Validar se usuário está ativo
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure("Usuário desativado");
            }

            // 4. Validar senha atual (suporta SHA256 e BCrypt)
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure("Senha atual inválida");
            }

            // 5. Validar se nova senha é diferente da atual
            if (request.NewPassword == request.CurrentPassword)
            {
                return Result<LoginResponse>.Failure("Nova senha não pode ser igual à senha atual");
            }

            // 6. Validar se nova senha não é a senha padrão (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            if (request.NewPassword == defaultPassword)
            {
                return Result<LoginResponse>.Failure($"Nova senha não pode ser a senha padrão (data de nascimento: {defaultPassword})");
            }

            // 7. Gerar hash BCrypt da nova senha (sempre BCrypt para novas senhas)
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.SetPassword(newPasswordHash);

            // 8. Atualizar senha (já marca IsFirstAccess = false)
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 9. Gerar JWT Token
            var token = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Name,
                user.CPF);

            var tokenExpiration = _jwtTokenService.GetTokenExpirationDate(token) ?? DateTime.UtcNow.AddHours(1);

            // 10. Retornar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                IsFirstAccess = user.IsFirstAccess,
                Token = token,
                TokenExpiration = tokenExpiration
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

    // TODO: Métodos futuros
    // public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    // public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
}
