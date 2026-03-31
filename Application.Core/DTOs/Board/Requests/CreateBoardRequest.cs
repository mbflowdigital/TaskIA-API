namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para criar uma nova tarefa
/// </summary>
public record CreateBoardRequest(
    Guid ProjectId,
    string Name,
    string? Description,
    string Status,
    string Priority,
    int PrazoEmDias,
    string? OrdemNoBoard,
    Guid? ResponsavelId,
    Guid? SugestaoResponsavelId
);
