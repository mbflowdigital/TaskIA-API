namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para adicionar dependência a uma tarefa
/// </summary>
public record AddDependencyRequest(
    Guid DependsOnBoardId
);
