using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repositório de dependências entre tarefas
/// </summary>
public class BoardDependencyRepository : Repository<BoardDependency>, IBoardDependencyRepository
{
    public BoardDependencyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BoardDependency>> GetDependenciesAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BoardDependency>()
            .Include(d => d.DependsOnBoard)
                .ThenInclude(b => b.Project)
            .Include(d => d.DependsOnBoard.Responsavel)
            .Where(d => d.BoardId == boardId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BoardDependency>> GetDependentTasksAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BoardDependency>()
            .Include(d => d.Board)
                .ThenInclude(b => b.Project)
            .Include(d => d.Board.Responsavel)
            .Where(d => d.DependsOnBoardId == boardId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BoardDependency>()
            .AnyAsync(d => d.BoardId == boardId && d.DependsOnBoardId == dependsOnBoardId, cancellationToken);
    }

    public async Task<bool> RemoveDependencyAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default)
    {
        var dependency = await _context.Set<BoardDependency>()
            .FirstOrDefaultAsync(d => d.BoardId == boardId && d.DependsOnBoardId == dependsOnBoardId, cancellationToken);

        if (dependency == null)
            return false;

        _context.Set<BoardDependency>().Remove(dependency);
        return true;
    }

    public async Task RemoveAllDependenciesAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var dependencies = await _context.Set<BoardDependency>()
            .Where(d => d.BoardId == boardId)
            .ToListAsync(cancellationToken);

        _context.Set<BoardDependency>().RemoveRange(dependencies);
    }

    public async Task<bool> WouldCreateCycleAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default)
    {
        // Se a tarefa "dependsOnBoardId" já depende direta ou indiretamente de "boardId", criar esta dependência formaria um ciclo
        var visited = new HashSet<Guid>();
        return await HasPathAsync(dependsOnBoardId, boardId, visited, cancellationToken);
    }

    /// <summary>
    /// Verifica se existe um caminho de dependências de 'from' para 'to' (algoritmo DFS)
    /// </summary>
    private async Task<bool> HasPathAsync(Guid from, Guid to, HashSet<Guid> visited, CancellationToken cancellationToken)
    {
        if (from == to)
            return true;

        if (visited.Contains(from))
            return false;

        visited.Add(from);

        // Obter todas as tarefas das quais 'from' depende
        var dependencies = await _context.Set<BoardDependency>()
            .Where(d => d.BoardId == from)
            .Select(d => d.DependsOnBoardId)
            .ToListAsync(cancellationToken);

        foreach (var dep in dependencies)
        {
            if (await HasPathAsync(dep, to, visited, cancellationToken))
                return true;
        }

        return false;
    }
}
