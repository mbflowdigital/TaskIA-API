namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para calcular a ordem ideal para uma tarefa em uma posição específica
/// </summary>
public record CalculateOrderRequest(
    Guid ProjectId,
    int TargetIndex,
    string? Status = null
);
