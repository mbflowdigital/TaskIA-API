namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para alterar a ordem da tarefa no Board
/// </summary>
public record UpdateBoardOrdemRequest(
    decimal OrdemNoBoard
);
