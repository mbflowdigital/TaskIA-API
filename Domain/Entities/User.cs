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

    public bool IsEmailVerified { get; set; }

    // Construtor público
    public User() { }

    public void UpdateProfile(string name, string? phone)
    {
        Name = name;
        Phone = phone;
        SetUpdatedAt();
    }

    public void SoftDelete()
    {
        Deactivate();
        SetUpdatedAt();
    }

    // TODO: Adicionar construtor com parâmetros e validações se necessário
    // public User(string name, string email, string? phone = null)
    // {
    //     // Validações aqui
    //     Name = name;
    //     Email = email;
    //     Phone = phone;
    // }

    // TODO: Adicionar métodos de negócio conforme necessário
    // public void UpdateEmail(string email) { }
    // public void VerifyEmail() { }
}
