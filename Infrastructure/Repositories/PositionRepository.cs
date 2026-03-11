using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly ApplicationDbContext _context;

    public PositionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PositionsEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .AsNoTracking()
            .OrderBy(position => position.PositionName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Positions.AnyAsync(position => position.Id == id, cancellationToken);
    }
}