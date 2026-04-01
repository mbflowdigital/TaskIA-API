namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para atualizar o prazo de uma tarefa
/// </summary>
public record UpdateBoardPrazoRequest(
    int PrazoEmDias
);
