using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repositório específico para Project
/// Herda Repository genérico e implementa métodos específicos de IProjectRepository
/// </summary>
public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Busca projetos por nome (busca parcial, case-insensitive)
    /// </summary>
    public async Task<IEnumerable<Project>> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        
        return await _dbSet
            .Include(p => p.User) // Incluir dados do usuário
            .Where(p => p.IsActive && p.Name.ToLower().Contains(normalizedName))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca projetos por status
    /// </summary>
    public async Task<IEnumerable<Project>> FindByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User) // Incluir dados do usuário
            .Where(p => p.IsActive && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Verifica se já existe projeto com o mesmo nome
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        
        return await _dbSet
            .AnyAsync(p => p.IsActive && p.Name.ToLower() == normalizedName, cancellationToken);
    }

    /// <summary>
    /// Busca apenas projetos ativos (com dados do usuário)
    /// </summary>
    public async Task<IEnumerable<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User) // Incluir dados do usuário
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca projetos de um usuário específico
    /// </summary>
    public async Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Verifica se usuário existe (para validação de FK)
    /// </summary>
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }

    /// <summary>
    /// Override GetByIdAsync para incluir dados do usuário
    /// </summary>
    public override async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
