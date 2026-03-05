using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Entidade de Empresa
/// </summary>
public class Company : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    public int NumberOfMembers { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    // Relacionamento com Users (uma empresa tem vários usuários)
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public Company() { }

    public void Update(string name, string? address, int numberOfMembers, string? category)
    {
        Name = name;
        Address = address;
        NumberOfMembers = numberOfMembers;
        Category = category;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        base.Deactivate();
        SetUpdatedAt();
    }
}
