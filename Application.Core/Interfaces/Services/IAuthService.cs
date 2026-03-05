using Application.Core.DTOs.Auth;
using Application.Core.DTOs.Users;
using Domain.Common;
using Domain.Enums;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do servi�o de autentica��o
/// Define contrato para l�gica de autentica��o de usu�rios
/// Preparado para evolu��o futura (JWT, refresh token, etc)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica usu�rio com CPF e senha
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
    /// Registra/cria um novo usuário.
    /// Observação: validação de permissão (403) deve ser feita no Controller com base nas Claims.
    /// </summary>
    Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        UserRole? createdByRole,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Completa o onboarding do ADM: cria empresa e vincula ao usuário.
    /// </summary>
    Task<Result<LoginResponse>> OnboardingAsync(
        OnboardingRequest request,
        CancellationToken cancellationToken = default);
    // TODO: M�todos futuros
    // Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    // Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
    // Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
