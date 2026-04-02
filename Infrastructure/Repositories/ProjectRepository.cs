using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Reposit�rio espec�fico para Project
/// Herda Repository gen�rico e implementa m�todos espec�ficos de IProjectRepository
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
            .Include(p => p.User) // Incluir dados do usu�rio
            .Include(p => p.Company)
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
            .Include(p => p.User) // Incluir dados do usu�rio
            .Include(p => p.Company)
            .Where(p => p.IsActive && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Verifica se j� existe projeto com o mesmo nome
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        
        return await _dbSet
            .AnyAsync(p => p.IsActive && p.Name.ToLower() == normalizedName, cancellationToken);
    }

    /// <summary>
    /// Busca apenas projetos ativos (com dados do usu�rio)
    /// </summary>
    public async Task<IEnumerable<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User) // Incluir dados do usu�rio
            .Include(p => p.Company)
            .Include(p => p.Tasks)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca projetos ativos por empresa
    /// </summary>
    public async Task<IEnumerable<Project>> GetActiveProjectsByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Company)
            .Include(p => p.Tasks)
            .Where(p => p.IsActive && p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca projetos de um usu�rio espec�fico
    /// </summary>
    public async Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Company)
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Verifica se usu�rio existe (para valida��o de FK)
    /// </summary>
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }

    /// <summary>
    /// Override GetByIdAsync para incluir dados do usu�rio e relacionamentos
    /// </summary>
    public override async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Company)
            .Include(p => p.ProjectMembers.Where(m => m.IsActive))
                .ThenInclude(m => m.User)
            .Include(p => p.ProjectDetails)
                .ThenInclude(pd => pd!.Compliances.Where(c => c.IsActive))
            .Include(p => p.ProjectDetails)
                .ThenInclude(pd => pd!.UnavailablePeriods.Where(up => up.IsActive))
            .Include(p => p.Dependencies.Where(d => d.IsActive))
            .Include(p => p.Integrations.Where(i => i.IsActive))
            .Include(p => p.SensitiveData.Where(s => s.IsActive))
            .Include(p => p.ExecutionSettings)
            .Include(p => p.PriorityRankings.Where(r => r.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddProjectDetailsAsync(ProjectDetails details, CancellationToken cancellationToken = default)
    {
        await _context.ProjectDetails.AddAsync(details, cancellationToken);
    }

    public async Task AddComplianceAsync(ProjectCompliance compliance, CancellationToken cancellationToken = default)
    {
        await _context.ProjectCompliances.AddAsync(compliance, cancellationToken);
    }

    public async Task AddUnavailablePeriodAsync(ProjectUnavailablePeriod period, CancellationToken cancellationToken = default)
    {
        await _context.ProjectUnavailablePeriods.AddAsync(period, cancellationToken);
    }

    public async Task<ProjectDetails?> GetProjectDetailsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectDetails
            .Include(pd => pd.Compliances.Where(c => c.IsActive))
            .Include(pd => pd.UnavailablePeriods.Where(up => up.IsActive))
            .FirstOrDefaultAsync(pd => pd.ProjectId == projectId, cancellationToken);
    }

    public async Task AddDependencyAsync(ProjectDependencies dependency, CancellationToken cancellationToken = default)
    {
        await _context.ProjectDependencies.AddAsync(dependency, cancellationToken);
    }

    public async Task AddIntegrationAsync(ProjectIntegrations integration, CancellationToken cancellationToken = default)
    {
        await _context.ProjectIntegrations.AddAsync(integration, cancellationToken);
    }

    public async Task AddSensitiveDataAsync(ProjectSensitiveData sensitiveData, CancellationToken cancellationToken = default)
    {
        await _context.ProjectSensitiveData.AddAsync(sensitiveData, cancellationToken);
    }

    public async Task AddExecutionSettingsAsync(ProjectExecutionSettings settings, CancellationToken cancellationToken = default)
    {
        await _context.ProjectExecutionSettings.AddAsync(settings, cancellationToken);
    }

    public async Task AddPriorityRankingAsync(ProjectPriorityRanking ranking, CancellationToken cancellationToken = default)
    {
        await _context.ProjectPriorityRankings.AddAsync(ranking, cancellationToken);
    }
}
