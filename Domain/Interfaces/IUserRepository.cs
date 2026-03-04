using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface específica para repositório de usuários
/// Extende IRepository com métodos específicos de User
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Busca usuário por email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se email já existe
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuário por CPF
    /// </summary>
    Task<User?> GetByCPFAsync(string cpf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se CPF já existe
    /// </summary>
    Task<bool> CPFExistsAsync(string cpf, CancellationToken cancellationToken = default);

    // TODO: Adicionar outros métodos específicos conforme necessário
    // Exemplo: Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}
