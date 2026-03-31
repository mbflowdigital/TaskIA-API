using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Representa uma dependência entre tarefas do Board
/// Uma tarefa (BoardId) depende de outra tarefa (DependsOnBoardId)
/// </summary>
public class BoardDependency : BaseEntity
{
    /// <summary>
    /// ID da tarefa que possui a dependência
    /// </summary>
    public Guid BoardId { get; private set; }

    /// <summary>
    /// ID da tarefa da qual depende (deve estar concluída)
    /// </summary>
    public Guid DependsOnBoardId { get; private set; }

    // Navigation Properties
    public Board Board { get; private set; } = null!;
    public Board DependsOnBoard { get; private set; } = null!;

    // Construtor vazio para EF Core
    private BoardDependency() { }

    public BoardDependency(Guid boardId, Guid dependsOnBoardId)
    {
        BoardId = boardId;
        DependsOnBoardId = dependsOnBoardId;
    }

    /// <summary>
    /// Verifica se a dependência está bloqueando a tarefa
    /// </summary>
    public bool IsBlocking()
    {
        return DependsOnBoard.Status != "Concluído";
    }
}
