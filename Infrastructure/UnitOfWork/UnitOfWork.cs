using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.UnitOfWork;

/// <summary>
/// Implementação do padrão Unit of Work
/// Gerencia transações e coordena operações do repositório
/// Garante que todas as mudanças sejam salvas atomicamente
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
