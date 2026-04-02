using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface específica para repositório de ProjectTasks
/// Herda operações genéricas e adiciona métodos específicos
/// </summary>
public interface IBoardRepository : IRepository<Board>
{
    Task<IEnumerable<Board>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Board>> GetByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Board>> GetByProjectIdAndPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default);
    Task<IEnumerable<Board>> GetByResponsavelAsync(string responsavel, CancellationToken cancellationToken = default);
    Task<IEnumerable<Board>> GetByResponsavelIdAsync(Guid responsavelId, CancellationToken cancellationToken = default);
    Task<Board?> GetByIdWithProjectAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<int> CountByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Board> boards, CancellationToken cancellationToken = default);
}
