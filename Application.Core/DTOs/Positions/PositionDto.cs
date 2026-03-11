namespace Application.Core.DTOs.Positions;

public record PositionDto
{
    public int Id { get; init; }
    public string PositionName { get; init; } = string.Empty;
    public string? Description { get; init; }
}