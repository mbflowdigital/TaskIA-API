using Domain.Entities;

namespace Domain.Interfaces;

public interface IPositionRepository
{
    Task<IEnumerable<PositionsEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}