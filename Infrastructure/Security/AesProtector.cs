using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

/// <summary>
/// Provides AES-256-CBC encryption/decryption for sensitive configuration values.
/// Allows storing encrypted secrets in appsettings.json without exposing raw keys in source control.
/// </summary>
internal static class AesProtector
{
    // Fixed 32-byte key for AES-256. Changing this invalidates all previously encrypted values.
    private static readonly byte[] Key = Convert.FromHexString("5461736B49412D323032362D4170694B65792D50726F74656374696F6E212121");

    /// <summary>
    /// Decrypts a Base64-encoded value that was encrypted with <see cref="Encrypt"/>.
    /// Returns the original plain text if the value does not appear to be encrypted (no Base64 padding / wrong length).
    /// </summary>
    public static string Decrypt(string encryptedBase64)
    {
        try
        {
            var allBytes = Convert.FromBase64String(encryptedBase64);
            if (allBytes.Length < 17) // IV (16) + at least 1 byte
                return encryptedBase64; // not encrypted, return as-is

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = allBytes[..16];

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(allBytes, 16, allBytes.Length - 16);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Value was not encrypted — return as-is (useful for local development overrides)
            return encryptedBase64;
        }
    }

    /// <summary>
    /// Encrypts a plain text value using AES-256-CBC and returns a Base64 string (IV + cipher).
    /// </summary>
    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }
}
