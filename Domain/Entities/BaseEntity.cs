namespace Domain.Entities;

/// <summary>
/// Entidade base para todas as entidades do domínio
/// Implementa propriedades comuns seguindo princípios SOLID
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    
    protected void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
