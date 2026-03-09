using Domain.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Serviço responsável por gerar e validar tokens JWT
/// Implementação de infraestrutura - não contém regras de negócio
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IMemoryCache _cache;
    private const string BlacklistPrefix = "jwt_blacklist_";

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, IMemoryCache cache)
    {
        _jwtSettings = jwtSettings.Value;
        _cache = cache;
    }

    /// <summary>
    /// Gera token JWT de acesso com claims do usuário
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string name, string cpf, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role),
            new("cpf", cpf),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gera refresh token aleatório criptograficamente seguro
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Extrai claims de um token expirado
    /// Útil para implementar refresh token flow
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateLifetime = false // Não valida expiração (token expirado)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256Signature,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Revoga um token JWT (adiciona na blacklist até expirar)
    /// Token revogado não pode mais ser usado mesmo que ainda seja válido
    /// </summary>
    public Task RevokeTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token não pode ser vazio", nameof(token));
        }

        var expirationDate = GetTokenExpirationDate(token);
        
        if (expirationDate == null)
        {
            throw new ArgumentException("Token inválido ou não possui data de expiração", nameof(token));
        }

        var cacheExpiration = expirationDate.Value - DateTime.UtcNow;

        // Se o token já expirou, não precisa adicionar na blacklist
        if (cacheExpiration.TotalSeconds > 0)
        {
            var key = GetBlacklistKey(token);
            _cache.Set(key, true, cacheExpiration);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifica se um token está revogado (na blacklist)
    /// </summary>
    public Task<bool> IsTokenRevokedAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(false);
        }

        var key = GetBlacklistKey(token);
        var isRevoked = _cache.TryGetValue(key, out _);
        
        return Task.FromResult(isRevoked);
    }

    /// <summary>
    /// Obtém a data de expiração de um token JWT
    /// </summary>
    public DateTime? GetTokenExpirationDate(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gera chave única para o token na blacklist usando hash SHA256
    /// Economiza memória ao armazenar apenas o hash ao invés do token completo
    /// </summary>
    private static string GetBlacklistKey(string token)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(token))
        );
        
        return $"{BlacklistPrefix}{tokenHash}";
    }
}