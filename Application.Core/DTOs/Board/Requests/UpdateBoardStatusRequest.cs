namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para alterar status da tarefa
/// </summary>
public record UpdateBoardStatusRequest(
    string Status
);
