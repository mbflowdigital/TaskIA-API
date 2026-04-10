using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repositório específico para ProjectTask
/// Herda Repository genérico e implementa métodos específicos de IProjectTaskRepository
/// </summary>
public class BoardRepository : Repository<Board>, IBoardRepository
{
    public BoardRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Busca todas as tarefas de um projeto
    /// </summary>
    public async Task<IEnumerable<Board>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Responsavel)
            .Include(t => t.SugestaoResponsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.Responsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.SugestaoResponsavel)
            .Where(t => t.ProjectId == projectId && t.IsActive)
            .OrderBy(t => t.OrdemNoBoard)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tarefas de um projeto por status
    /// </summary>
    public async Task<IEnumerable<Board>> GetByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Responsavel)
            .Include(t => t.SugestaoResponsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.Responsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.SugestaoResponsavel)
            .Where(t => t.ProjectId == projectId && t.Status == status && t.IsActive)
            .OrderBy(t => t.OrdemNoBoard)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tarefas de um projeto por prioridade
    /// </summary>
    public async Task<IEnumerable<Board>> GetByProjectIdAndPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Responsavel)
            .Include(t => t.SugestaoResponsavel)
            .Where(t => t.ProjectId == projectId && t.Priority == priority && t.IsActive)
            .OrderBy(t => t.OrdemNoBoard)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tarefas por responsável (ID do usuário)
    /// </summary>
    public async Task<IEnumerable<Board>> GetByResponsavelAsync(string responsavel, CancellationToken cancellationToken = default)
    {
        // Tenta converter para Guid se for um ID
        if (Guid.TryParse(responsavel, out var responsavelId))
        {
            return await _dbSet
                .Include(t => t.Project)
                .Include(t => t.Responsavel)
                .Where(t => t.IsActive && t.ResponsavelId == responsavelId)
                .OrderBy(t => t.PrazoEmDias)
                .ThenBy(t => t.Priority)
                .ToListAsync(cancellationToken);
        }

        // Se não for Guid, busca por nome no responsável ou na sugestão
        var normalizedResponsavel = responsavel.Trim().ToLower();
        return await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Responsavel)
            .Include(t => t.SugestaoResponsavel)
            .Where(t => t.IsActive && 
                   (t.SugestaoResponsavel != null && t.SugestaoResponsavel.Name.ToLower().Contains(normalizedResponsavel) ||
                    t.Responsavel != null && t.Responsavel.Name.ToLower().Contains(normalizedResponsavel)))
            .OrderBy(t => t.PrazoEmDias)
            .ThenBy(t => t.Priority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tarefas por ID do responsável
    /// </summary>
    public async Task<IEnumerable<Board>> GetByResponsavelIdAsync(Guid responsavelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Responsavel)
            .Where(t => t.IsActive && t.ResponsavelId == responsavelId)
            .OrderBy(t => t.PrazoEmDias)
            .ThenBy(t => t.Priority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tarefa por ID com informações do projeto
    /// </summary>
    public async Task<Board?> GetByIdWithProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Responsavel)
            .Include(t => t.SugestaoResponsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.Responsavel)
            .Include(t => t.SubTasks)
                .ThenInclude(st => st.SugestaoResponsavel)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
    }

    /// <summary>
    /// Conta tarefas de um projeto
    /// </summary>
    public async Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(t => t.ProjectId == projectId && t.IsActive, cancellationToken);
    }

    /// <summary>
    /// Conta tarefas de um projeto por status
    /// </summary>
    public async Task<int> CountByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(t => t.ProjectId == projectId && t.Status == status && t.IsActive, cancellationToken);
    }

    /// <summary>
    /// Adiciona múltiplas tarefas em lote
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<Board> boards, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(boards, cancellationToken);
    }
}
