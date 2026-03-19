namespace Infrastructure.Configuration;

/// <summary>
/// Configurações JWT extraídas do appsettings.json
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Chave secreta para assinatura do token (mínimo 32 caracteres)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Emissor do token
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiência do token
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de expiração do access token em minutos
    /// </summary>
    public int ExpirationInMinutes { get; set; }

    /// <summary>
    /// Tempo de expiração do refresh token em dias
    /// </summary>
    public int RefreshTokenExpirationInDays { get; set; }
}