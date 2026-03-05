using Application.Core.DTOs.Auth;
using Domain.Common;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do serviço de autenticação
/// Define contrato para lógica de autenticação de usuários
/// Preparado para evolução futura (JWT, refresh token, etc)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica usuário com CPF e senha
    /// </summary>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se CPF existe no sistema
    /// </summary>
    Task<bool> CPFExistsAsync(string cpf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Troca senha de primeiro acesso
    /// </summary>
    Task<Result<LoginResponse>> ChangePasswordFirstAccessAsync(ChangePasswordFirstAccessRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Realiza logout do usuário revogando o token JWT
    /// </summary>
    Task<Result> LogoutAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renova token JWT usando refresh token
    /// </summary>
    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Troca senha do usuário autenticado
    /// </summary>
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reseta senha usando CPF (esqueceu a senha)
    /// Aplica mesmas validações de troca de senha
    /// </summary>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
}
