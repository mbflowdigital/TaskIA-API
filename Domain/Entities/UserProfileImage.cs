using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade para armazenar imagens de perfil dos usuários
/// Suporta formatos: JPG, PNG, WEBP
/// Tamanho máximo: 5 MB
/// </summary>
[Table("UserProfileImages")]
public class UserProfileImage : BaseEntity
{
    /// <summary>
    /// ID do usuário proprietário da imagem
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Dados binários da imagem
    /// Tamanho máximo: 5 MB (5.242.880 bytes)
    /// Formatos suportados: JPG, PNG, WEBP
    /// </summary>
    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Tipo MIME da imagem (image/jpeg, image/png, image/webp)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    [Required]
    public long FileSizeBytes { get; set; }

    // Navegação
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
