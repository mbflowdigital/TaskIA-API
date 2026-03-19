namespace Domain.Interfaces;

/// <summary>
/// Interface para serviço de hash de senha
/// Suporta migração híbrida de SHA256 para BCrypt
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera hash seguro da senha usando BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifica senha suportando SHA256 (legado) e BCrypt (novo)
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Verifica se o hash é BCrypt (novo formato)
    /// </summary>
    bool IsBcryptHash(string passwordHash);
}