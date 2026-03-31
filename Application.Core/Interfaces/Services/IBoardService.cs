using Application.Core.DTOs.Board.Requests;
using Application.Core.DTOs.Board.Responses;
using Domain.Common;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface para serviço de gerenciamento de Board (Tarefas)
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// Obtém todas as tarefas de um projeto
    /// </summary>
    Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém tarefas de um projeto por status
    /// </summary>
    Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém tarefas de um projeto por prioridade
    /// </summary>
    Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAndPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma tarefa por ID
    /// </summary>
    Task<Result<BoardDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria uma nova tarefa
    /// </summary>
    Task<Result<BoardDto>> CreateAsync(CreateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza informações de uma tarefa
    /// </summary>
    Task<Result<BoardDto>> UpdateAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atribui ou altera o responsável de uma tarefa
    /// </summary>
    Task<Result<BoardDto>> AssignResponsavelAsync(Guid id, AssignResponsavelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atribui ou altera a sugestão de responsável de uma tarefa
    /// </summary>
    Task<Result<BoardDto>> AssignSugestaoResponsavelAsync(Guid id, AssignSugestaoResponsavelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Altera o status de uma tarefa
    /// </summary>
    Task<Result<BoardDto>> UpdateStatusAsync(Guid id, UpdateBoardStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Altera a ordem da tarefa no board
    /// </summary>
    Task<Result<BoardDto>> UpdateOrdemAsync(Guid id, UpdateBoardOrdemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma tarefa (soft delete)
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém estatísticas de tarefas por projeto
    /// </summary>
    Task<Result<BoardStatisticsDto>> GetStatisticsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
