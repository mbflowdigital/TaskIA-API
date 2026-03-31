using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface do repositório de dependências entre tarefas
/// </summary>
public interface IBoardDependencyRepository : IRepository<BoardDependency>
{
    /// <summary>
    /// Obtém todas as dependências de uma tarefa (tarefas das quais ela depende)
    /// </summary>
    Task<IEnumerable<BoardDependency>> GetDependenciesAsync(Guid boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as tarefas que dependem de uma tarefa específica
    /// </summary>
    Task<IEnumerable<BoardDependency>> GetDependentTasksAsync(Guid boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe uma dependência entre duas tarefas
    /// </summary>
    Task<bool> ExistsAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma dependência específica
    /// </summary>
    Task<bool> RemoveDependencyAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove todas as dependências de uma tarefa
    /// </summary>
    Task RemoveAllDependenciesAsync(Guid boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a criação de uma dependência criaria um ciclo
    /// </summary>
    Task<bool> WouldCreateCycleAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default);
}
