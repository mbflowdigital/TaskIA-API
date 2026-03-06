using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface espec�fica para reposit�rio de Projects
/// Herda opera��es gen�ricas e adiciona m�todos espec�ficos
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
    /// Verifica se j� existe projeto com o mesmo nome (para o mesmo usu�rio no futuro)
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos ativos
    /// </summary>
    Task<IEnumerable<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos de um usu�rio espec�fico
    /// </summary>
    Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca projetos ativos de uma empresa específica
    /// </summary>
    Task<IEnumerable<Project>> GetActiveProjectsByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se usu�rio existe (FK constraint)
    /// </summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
