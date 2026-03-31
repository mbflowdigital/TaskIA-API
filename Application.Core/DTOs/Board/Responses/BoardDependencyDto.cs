namespace Application.Core.DTOs.Board.Responses;

/// <summary>
/// DTO para informação de dependência
/// </summary>
public record BoardDependencyDto(
    Guid Id,
    Guid BoardId,
    Guid DependsOnBoardId,
    string DependsOnBoardName,
    string DependsOnBoardStatus,
    bool IsBlocking
);

/// <summary>
/// DTO para informações sobre bloqueios de uma tarefa
/// </summary>
public record BoardBlockingInfoDto(
    bool IsBlocked,
    int BlockingDependenciesCount,
    List<BoardDependencyDto> BlockingDependencies
);
