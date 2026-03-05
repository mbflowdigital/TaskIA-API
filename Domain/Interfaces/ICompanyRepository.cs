using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface específica para repositório de empresas
/// </summary>
public interface ICompanyRepository : IRepository<Company>
{
    /// <summary>
    /// Lista todas as empresas ativas com seus usuários
    /// </summary>
    Task<IEnumerable<Company>> GetAllWithUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca empresa por ID incluindo usuários
    /// </summary>
    Task<Company?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se nome já está em uso
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
