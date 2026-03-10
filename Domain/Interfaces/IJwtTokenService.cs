using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Domain.Interfaces;

/// <summary>
/// Interface para serviço de gerenciamento de tokens JWT
/// Inclui geração, validação e revogação de tokens
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string name, string cpf);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    Task RevokeTokenAsync(string token);

    Task<bool> IsTokenRevokedAsync(string token);

    DateTime? GetTokenExpirationDate(string token);
}
