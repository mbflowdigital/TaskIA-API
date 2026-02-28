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
        CreatedAt = GetBrazilianTime();
        IsActive = true;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    
    protected void SetUpdatedAt()
    {
        UpdatedAt = GetBrazilianTime();
    }

    /// <summary>
    /// Obtém a hora atual no fuso horário de Brasília (UTC-3)
    /// </summary>
    private static DateTime GetBrazilianTime()
    {
        var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone);
    }
}
