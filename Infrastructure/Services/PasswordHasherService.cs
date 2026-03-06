using Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Serviço de hash de senha com migração híbrida SHA256 ? BCrypt
/// 
/// ESTRATÉGIA DE MIGRAÇÃO(Provisória):
/// 1. Senhas antigas: SHA256 (Base64, ~44 caracteres)
/// 2. Senhas novas: BCrypt (começa com $2a$, $2b$ ou $2y$, ~60 caracteres)
/// 3. No login, detecta o tipo e migra automaticamente para BCryp
/// </summary>
public class PasswordHasherService : IPasswordHasher
{
    private const int BcryptWorkFactor = 12;

    /// <summary>
    /// Gera hash BCrypt da senha
    /// Todas as novas senhas usarão BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Senha não pode ser vazia", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);
    }

    /// <summary>
    /// Verifica senha suportando SHA256 (legado) e BCrypt (novo)
    /// Detecta automaticamente o formato do hash
    /// </summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            // Detectar se é BCrypt (novo formato)
            if (IsBcryptHash(passwordHash))
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }

            // Fallback para SHA256 (legado)
            return VerifySHA256Password(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica se o hash é BCrypt
    /// BCrypt hash começa com $2a$, $2b$ ou $2y$ e tem ~60 caracteres
    /// </summary>
    public bool IsBcryptHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        // BCrypt hash tem formato: $2a$12$... ou $2b$12$... ou $2y$12$...
        return passwordHash.StartsWith("$2a$") || 
               passwordHash.StartsWith("$2b$") || 
               passwordHash.StartsWith("$2y$");
    }

    /// <summary>
    /// Verifica senha usando SHA256 (sistema legado)
    /// Usado apenas para senhas antigas durante a migração
    /// </summary>
    private static bool VerifySHA256Password(string password, string passwordHash)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        var computedHash = Convert.ToBase64String(hash);
        
        return computedHash == passwordHash;
    }
}