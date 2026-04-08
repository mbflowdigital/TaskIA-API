namespace Application.Core.DTOs.Users;

/// <summary>
/// Request para upload de imagem de perfil
/// </summary>
public record UploadProfileImageRequest
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Dados binários da imagem
    /// </summary>
    public byte[] ImageData { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Tipo MIME da imagem (image/jpeg, image/png, image/webp)
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Nome do arquivo original
    /// </summary>
    public string FileName { get; init; } = string.Empty;
}

/// <summary>
/// Response após upload de imagem de perfil
/// </summary>
public record ProfileImageDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
}
