namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para atualizar informações da tarefa
/// </summary>
public record UpdateBoardRequest(
    string Name,
    string? Description,
    string Priority,
    int PrazoEmDias,
    string? OrdemNoBoard
);
