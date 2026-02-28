using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade de Projeto
/// Representa um projeto gerenciado pelo sistema
/// </summary>
public class Project : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Objective { get; set; }

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Active, Paused, Completed, Cancelled

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Relacionamento com User
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    // Construtor público
    public Project() { }

    /// <summary>
    /// Atualiza informações básicas do projeto
    /// </summary>
    public void UpdateInfo(string name, string? objective, string? description, DateTime? startDate, DateTime? endDate)
    {
        Name = name;
        Objective = objective;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        SetUpdatedAt();
    }

    /// <summary>
    /// Atualiza status do projeto
    /// </summary>
    public void UpdateStatus(string status)
    {
        if (IsValidStatus(status))
        {
            Status = status;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Valida se o status é válido
    /// </summary>
    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled" };
        return validStatuses.Contains(status);
    }

    /// <summary>
    /// Soft delete do projeto
    /// </summary>
    public void SoftDelete()
    {
        Deactivate();
        SetUpdatedAt();
    }
}
