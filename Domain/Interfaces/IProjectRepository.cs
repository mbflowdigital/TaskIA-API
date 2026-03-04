using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface específica para repositório de Projects
/// Herda operações genéricas e adiciona métodos específicos
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    /// <summary>
    /// Busca projetos por nome (busca parcial)
    /// </summary>
    Task<IEnumerable<Project>> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos por status
    /// </summary>
    Task<IEnumerable<Project>> FindByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe projeto com o mesmo nome (para o mesmo usuário no futuro)
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos ativos
    /// </summary>
    Task<IEnumerable<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos de um usuário específico
    /// </summary>
    Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se usuário existe (FK constraint)
    /// </summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
