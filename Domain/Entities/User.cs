using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Entidade de Usuário
/// TODO: Implementar validações e métodos de negócio conforme necessário
/// </summary>
public class User : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(11)]
    public string CPF { get; set; } = string.Empty;

    [Required]
    public DateTime BirthDate { get; set; }

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public bool IsFirstAccess { get; set; } = true;

    // Relacionamento com Projects (um usuário pode ter vários projetos)
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    // Construtor público
    public User() { }

    public void UpdateProfile(string name, string? phone)
    {
        Name = name;
        Phone = phone;
        SetUpdatedAt();
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        IsFirstAccess = false;
        SetUpdatedAt();
    }

    /// <summary>
    /// Obtém a senha padrão baseada na data de nascimento (ddMMyyyy)
    /// Exemplo: 25/11/1998 = "25111998"
    /// </summary>
    public string GetDefaultPassword()
    {
        return BirthDate.ToString("ddMMyyyy");
    }

    public void SoftDelete()
    {
        Deactivate();
        SetUpdatedAt();
    }
}
