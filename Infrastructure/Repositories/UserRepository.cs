using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repositório específico para User
/// Herda Repository genérico e implementa métodos específicos de IUserRepository
/// Este é um EXEMPLO para os desenvolvedores criarem outros repositórios específicos
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Override: Sempre carrega Role junto com User
    /// </summary>
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Override: Sempre carrega Role junto com User
    /// </summary>
    public override async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca usuário por email com Role
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Verifica se email já existe no banco
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Busca usuário por CPF
    /// </summary>
    public async Task<User?> GetByCPFAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var normalizedCPF = cpf.Replace(".", "").Replace("-", "").Trim();
        
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.CPF == normalizedCPF && u.IsActive, cancellationToken);
    }

    /// <summary>
    /// Verifica se CPF já existe no banco
    /// </summary>
    public async Task<bool> CPFExistsAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var normalizedCPF = cpf.Replace(".", "").Replace("-", "").Trim();
        
        return await _dbSet
            .AnyAsync(u => u.CPF == normalizedCPF, cancellationToken);
    }

    /// <summary>
    /// Lista usuários de uma empresa específica
    /// </summary>
    public async Task<IEnumerable<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    // TODO: Exemplo de método específico que pode ser adicionado
    // public async Task<IEnumerable<User>> GetVerifiedUsersAsync(CancellationToken cancellationToken = default)
    // {
    //     return await _dbSet
    //         .Where(u => u.IsEmailVerified)
    //         .OrderBy(u => u.Name)
    //         .ToListAsync(cancellationToken);
    // }
}
