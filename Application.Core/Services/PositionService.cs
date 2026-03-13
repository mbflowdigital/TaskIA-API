using Application.Core.DTOs.Positions;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Interfaces;

namespace Application.Core.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _positionRepository;

    public PositionService(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<Result<IEnumerable<PositionDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var positions = await _positionRepository.GetAllAsync(cancellationToken);
            var data = positions
                .Select(position => new PositionDto
                {
                    Id = position.Id,
                    PositionName = position.PositionName,
                    Description = position.Description
                })
                .ToList();

            return Result<IEnumerable<PositionDto>>.Success(data, $"{data.Count} posição(ões) encontrada(s)");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PositionDto>>.Failure($"Erro ao listar posições: {ex.Message}");
        }
    }
}