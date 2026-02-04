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

    /// <summary>
    /// Busca usuário por email
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
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

    // TODO: Exemplo de método específico que pode ser adicionado
    // public async Task<IEnumerable<User>> GetVerifiedUsersAsync(CancellationToken cancellationToken = default)
    // {
    //     return await _dbSet
    //         .Where(u => u.IsEmailVerified)
    //         .OrderBy(u => u.Name)
    //         .ToListAsync(cancellationToken);
    // }
}
