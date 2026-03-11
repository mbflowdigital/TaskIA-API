using Application.Core.DTOs.Positions;
using Domain.Common;

namespace Application.Core.Interfaces.Services;

public interface IPositionService
{
    Task<Result<IEnumerable<PositionDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}