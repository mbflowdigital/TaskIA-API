using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Entidade de Usuário
/// TODO: Implementar validações e métodos de negócio
/// </summary>
public class User : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; private set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; private set; }

    [MaxLength(20)]
    public string? Phone { get; private set; }

    public bool IsEmailVerified { get; private set; }

    // Construtor privado para EF Core
    private User() 
    { 
        Name = string.Empty;
        Email = string.Empty;
    }

    // TODO: Implementar construtor público com validações
    // TODO: Implementar métodos de atualização (UpdateEmail, UpdatePhone, etc)
}
