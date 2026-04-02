namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para calcular a ordem de uma tarefa relativa a outra tarefa de referência
/// Útil para drag-and-drop
/// </summary>
public record CalculateOrderRelativeRequest(
    Guid ProjectId,
    decimal ReferenceOrderValue,
    bool InsertBefore,
    string? Status = null
);
