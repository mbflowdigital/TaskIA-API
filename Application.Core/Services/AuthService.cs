using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Application.Core.Services;

/// <summary>
/// Service de Autenticação
/// Contém toda a lógica de autenticação de usuários
/// Senha padrão: Data de nascimento no formato ddMMyyyy (ex: 25111998)
/// Preparado para evolução futura (JWT, refresh token, hash bcrypt, etc)
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Autentica usuário com CPF e senha
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

            // 4. Validar senha
            var passwordHash = HashPassword(request.Password);
            if (user.PasswordHash != passwordHash)
            {
                return Result<LoginResponse>.Failure("CPF ou senha inválidos");
            }

            // 5. Criar response
            var response = new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                CPF = user.CPF,
                Phone = user.Phone,
                IsFirstAccess = user.IsFirstAccess,
                
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

            // 4. Validar senha atual
            var currentPasswordHash = HashPassword(request.CurrentPassword);
            if (user.PasswordHash != currentPasswordHash)
            {
                return Result<LoginResponse>.Failure("Senha atual inválida");
            }

            // 5. Validar se nova senha é diferente da atual
            var newPasswordHash = HashPassword(request.NewPassword);
            if (user.PasswordHash == newPasswordHash)
            {
                return Result<LoginResponse>.Failure("Nova senha não pode ser igual à senha atual");
            }

            // 6. Validar se nova senha não é a senha padrão (data de nascimento)
            var defaultPassword = user.GetDefaultPassword();
            var defaultPasswordHash = HashPassword(defaultPassword);
            if (newPasswordHash == defaultPasswordHash)
            {
                return Result<LoginResponse>.Failure($"Nova senha não pode ser a senha padrão (data de nascimento: {defaultPassword})");
            }

            // 7. Atualizar senha (já marca IsFirstAccess = false)
            user.SetPassword(newPasswordHash);

            // 8. Persistir alterações
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
                IsFirstAccess = user.IsFirstAccess, // Agora é false
                Token = null,
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

    // TODO: Métodos futuros
    // public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    // public async Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    // public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
}
