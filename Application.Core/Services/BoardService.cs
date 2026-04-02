using Application.Core.DTOs.Board.Extensions;
using Application.Core.DTOs.Board.Requests;
using Application.Core.DTOs.Board.Responses;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Core.Services;

/// <summary>
/// Serviço de gerenciamento de Board (Tarefas)
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BoardService(
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return Result<IEnumerable<BoardDto>>.Failure("Projeto não encontrado.");

        var boards = await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var boardDtos = boards.ToDto();

        return Result<IEnumerable<BoardDto>>.Success(boardDtos);
    }

    public async Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAndStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return Result<IEnumerable<BoardDto>>.Failure("Projeto não encontrado.");

        if (!IsValidStatus(status))
            return Result<IEnumerable<BoardDto>>.Failure("Status inválido. Valores aceitos: A Fazer, Em Andamento, Concluído");

        var boards = await _boardRepository.GetByProjectIdAndStatusAsync(projectId, status, cancellationToken);
        var boardDtos = boards.ToDto();

        return Result<IEnumerable<BoardDto>>.Success(boardDtos);
    }

    public async Task<Result<IEnumerable<BoardDto>>> GetByProjectIdAndPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return Result<IEnumerable<BoardDto>>.Failure("Projeto não encontrado.");

        if (!IsValidPriority(priority))
            return Result<IEnumerable<BoardDto>>.Failure("Prioridade inválida. Valores aceitos: Baixa, Média, Alta, Crítica");

        var boards = await _boardRepository.GetByProjectIdAndPriorityAsync(projectId, priority, cancellationToken);
        var boardDtos = boards.ToDto();

        return Result<IEnumerable<BoardDto>>.Success(boardDtos);
    }

    public async Task<Result<BoardDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        var boardDto = board.ToDto();
        return Result<BoardDto>.Success(boardDto);
    }

    public async Task<Result<BoardDto>> CreateAsync(CreateBoardRequest request, CancellationToken cancellationToken = default)
    {
        // Validar projeto
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<BoardDto>.Failure("Projeto não encontrado.");

        // Validar status
        if (!IsValidStatus(request.Status))
            return Result<BoardDto>.Failure("Status inválido. Valores aceitos: A Fazer, Em Andamento, Concluído");

        // Validar prioridade
        if (!IsValidPriority(request.Priority))
            return Result<BoardDto>.Failure("Prioridade inválida. Valores aceitos: Baixa, Média, Alta, Crítica");

        User? responsavel = null;
        User? sugestao = null;

        // Validar responsável se fornecido
        if (request.ResponsavelId.HasValue)
        {
            responsavel = await _userRepository.GetByIdAsync(request.ResponsavelId.Value, cancellationToken);
            if (responsavel == null)
                return Result<BoardDto>.Failure("Responsável não encontrado.");
        }

        // Validar sugestão de responsável se fornecido
        if (request.SugestaoResponsavelId.HasValue)
        {
            sugestao = await _userRepository.GetByIdAsync(request.SugestaoResponsavelId.Value, cancellationToken);
            if (sugestao == null)
                return Result<BoardDto>.Failure("Usuário sugerido não encontrado.");
        }

        // Se ordem não foi fornecida, calcular automaticamente para inserir no final
        // Não filtra por status para garantir unicidade no projeto inteiro
        var ordem = request.OrdemNoBoard;
        if (ordem == 0)
        {
            ordem = await CalculateOrderAsync(request.ProjectId, status: null, cancellationToken: cancellationToken);
        }

        var board = new Board(
            projectId: request.ProjectId,
            name: request.Name,
            description: request.Description,
            status: request.Status,
            priority: request.Priority,
            sugestaoResponsavelId: request.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: ordem
        );

        if (request.ResponsavelId.HasValue)
        {
            board.AssignResponsavel(request.ResponsavelId.Value);
        }

        await _boardRepository.AddAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Carregar relacionamentos manualmente para evitar segunda query
        board.Project = project;
        if (responsavel != null)
            board.Responsavel = responsavel;
        if (sugestao != null)
            board.SugestaoResponsavel = sugestao;

        var boardDto = board.ToDto();

        return Result<BoardDto>.Success(boardDto, "Tarefa criada com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        if (!IsValidPriority(request.Priority))
            return Result<BoardDto>.Failure("Prioridade inválida. Valores aceitos: Baixa, Média, Alta, Crítica");

        // Se a tarefa não está com status "A Fazer", validar a mudança de prioridade
        // Para evitar que uma tarefa em andamento ou concluída seja rebaixada indevidamente
        if (board.Status != "A Fazer")
        {
            // Validar se a nova prioridade é permitida baseado no estado atual das outras tarefas
            var validacaoPrioridade = await ValidarAlteracaoStatusPorPrioridadeAsync(board.ProjectId, request.Priority, cancellationToken);
            if (!validacaoPrioridade.IsSuccess)
                return Result<BoardDto>.Failure($"Não é possível alterar a prioridade para '{request.Priority}'. {validacaoPrioridade.Message}");
        }

        // Se ordem não foi fornecida, manter a ordem atual
        var ordem = request.OrdemNoBoard == 0 ? board.OrdemNoBoard : request.OrdemNoBoard;

        board.UpdateInfo(
            name: request.Name,
            description: request.Description,
            sugestaoResponsavelId: board.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: ordem
        );

        board.UpdatePriority(request.Priority);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = updatedBoard.ToDto();

        return Result<BoardDto>.Success(boardDto, "Tarefa atualizada com sucesso.");
    }

    public async Task<Result<BoardDto>> AssignResponsavelAsync(Guid id, AssignResponsavelRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        // Validar usuário se fornecido
        if (request.ResponsavelId.HasValue)
        {
            var responsavel = await _userRepository.GetByIdAsync(request.ResponsavelId.Value, cancellationToken);
            if (responsavel == null)
                return Result<BoardDto>.Failure("Responsável não encontrado.");
        }

        board.AssignResponsavel(request.ResponsavelId);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = updatedBoard.ToDto();

        return Result<BoardDto>.Success(boardDto, "Responsável atribuído com sucesso.");
    }

    public async Task<Result<BoardDto>> AssignSugestaoResponsavelAsync(Guid id, AssignSugestaoResponsavelRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        // Validar usuário se fornecido
        if (request.SugestaoResponsavelId.HasValue)
        {
            var sugestao = await _userRepository.GetByIdAsync(request.SugestaoResponsavelId.Value, cancellationToken);
            if (sugestao == null)
                return Result<BoardDto>.Failure("Usuário sugerido não encontrado.");
        }

        board.AssignSugestaoResponsavel(request.SugestaoResponsavelId);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = updatedBoard.ToDto();

        return Result<BoardDto>.Success(boardDto, "Sugestão de responsável atribuída com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateStatusAsync(Guid id, UpdateBoardStatusRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        if (!IsValidStatus(request.Status))
            return Result<BoardDto>.Failure("Status inválido. Valores aceitos: A Fazer, Em Andamento, Concluído");

        // Validar regra de prioridade antes de alterar o status
        var validacaoPrioridade = await ValidarAlteracaoStatusPorPrioridadeAsync(board.ProjectId, board.Priority, cancellationToken);
        if (!validacaoPrioridade.IsSuccess)
            return Result<BoardDto>.Failure(validacaoPrioridade.Message);

        board.UpdateStatus(request.Status);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = updatedBoard.ToDto();

        return Result<BoardDto>.Success(boardDto, "Status atualizado com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateOrdemAsync(Guid id, UpdateBoardOrdemRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        // Se ordem não foi fornecida, calcular automaticamente para inserir no final
        // Não filtra por status para garantir unicidade no projeto inteiro
        var ordem = request.OrdemNoBoard;
        if (ordem == 0)
        {
            ordem = await CalculateOrderAsync(board.ProjectId, status: null, cancellationToken: cancellationToken);
        }

        board.UpdateOrdemNoBoard(ordem);
        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var boardDto = board.ToDto();

        return Result<BoardDto>.Success(boardDto, "Ordem atualizada com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdatePrazoAsync(Guid id, UpdateBoardPrazoRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        if (request.PrazoEmDias <= 0)
            return Result<BoardDto>.Failure("O prazo deve ser maior que zero.");

        board.UpdateInfo(
            name: board.Name,
            description: board.Description,
            sugestaoResponsavelId: board.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: board.OrdemNoBoard
        );

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = updatedBoard.ToDto();

        return Result<BoardDto>.Success(boardDto, "Prazo atualizado com sucesso.");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result.Failure("Tarefa não encontrada.");

        board.SoftDelete();

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success("Tarefa removida com sucesso.");
    }

    public async Task<Result<BoardStatisticsDto>> GetStatisticsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return Result<BoardStatisticsDto>.Failure("Projeto não encontrado.");

        var boards = await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var boardsList = boards.ToList();

        var statistics = new BoardStatisticsDto(
            TotalTasks: boardsList.Count,
            TasksAFazer: boardsList.Count(b => b.Status == "A Fazer"),
            TasksEmAndamento: boardsList.Count(b => b.Status == "Em Andamento"),
            TasksConcluidas: boardsList.Count(b => b.Status == "Concluído"),
            TasksBaixaPrioridade: boardsList.Count(b => b.Priority == "Baixa"),
            TasksMediaPrioridade: boardsList.Count(b => b.Priority == "Média"),
            TasksAltaPrioridade: boardsList.Count(b => b.Priority == "Alta"),
            TasksCriticaPrioridade: boardsList.Count(b => b.Priority == "Crítica")
        );

        return Result<BoardStatisticsDto>.Success(statistics);
    }

    // Métodos auxiliares
    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "A Fazer", "Em Andamento", "Concluído" };
        return validStatuses.Contains(status);
    }

    private static bool IsValidPriority(string priority)
    {
        var validPriorities = new[] { "Baixa", "Média", "Alta", "Crítica" };
        return validPriorities.Contains(priority);
    }

    /// <summary>
    /// Valida se uma tarefa pode ter seu status alterado baseado na prioridade e no estado das outras tarefas do projeto
    /// Regra: Tarefas de menor prioridade só podem ser alteradas se todas as de maior prioridade estiverem concluídas
    /// </summary>
    private async Task<Result> ValidarAlteracaoStatusPorPrioridadeAsync(Guid projectId, string prioridade, CancellationToken cancellationToken)
    {
        // Tarefas Críticas e Altas podem ser alteradas sempre
        if (prioridade == "Crítica" || prioridade == "Alta")
            return Result.Success();

        // Buscar todas as tarefas ativas do projeto
        var todasTarefas = await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var tarefasList = todasTarefas.ToList();

        // Para tarefas de prioridade Média: verificar se existem tarefas Críticas ou Altas não concluídas
        if (prioridade == "Média")
        {
            var tarefasAltaPrioridadeNaoConcluidas = tarefasList
                .Where(t => (t.Priority == "Crítica" || t.Priority == "Alta") && t.Status != "Concluído")
                .ToList();

            if (tarefasAltaPrioridadeNaoConcluidas.Any())
            {
                var count = tarefasAltaPrioridadeNaoConcluidas.Count;
                var prioridades = string.Join(", ", tarefasAltaPrioridadeNaoConcluidas.Select(t => t.Priority).Distinct());
                return Result.Failure($"Não é possível alterar o status desta tarefa de prioridade Média. Existem {count} tarefa(s) de prioridade {prioridades} não concluída(s).");
            }
        }

        // Para tarefas de prioridade Baixa: verificar se existem tarefas Críticas, Altas ou Médias não concluídas
        if (prioridade == "Baixa")
        {
            var tarefasMaiorPrioridadeNaoConcluidas = tarefasList
                .Where(t => (t.Priority == "Crítica" || t.Priority == "Alta" || t.Priority == "Média") && t.Status != "Concluído")
                .ToList();

            if (tarefasMaiorPrioridadeNaoConcluidas.Any())
            {
                var count = tarefasMaiorPrioridadeNaoConcluidas.Count;
                var prioridades = string.Join(", ", tarefasMaiorPrioridadeNaoConcluidas.Select(t => t.Priority).Distinct());
                return Result.Failure($"Não é possível alterar o status desta tarefa de prioridade Baixa. Existem {count} tarefa(s) de prioridade {prioridades} não concluída(s).");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Calcula a ordem para uma tarefa usando estratégia de média decimal.
    /// Suporta múltiplas estratégias: por índice, relativa a referência, ou no final da lista.
    /// </summary>
    /// <param name="projectId">ID do projeto</param>
    /// <param name="status">Status da coluna (opcional)</param>
    /// <param name="targetIndex">Índice de destino (para estratégia por posição)</param>
    /// <param name="referenceOrderValue">Valor de ordem de referência (para estratégia relativa)</param>
    /// <param name="insertBefore">Inserir antes da referência (para estratégia relativa)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Valor decimal da nova ordem</returns>
    private async Task<decimal> CalculateOrderAsync(
        Guid projectId,
        string? status = null,
        int? targetIndex = null,
        decimal? referenceOrderValue = null,
        bool insertBefore = false,
        CancellationToken cancellationToken = default)
    {
        var query = await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var orderedBoards = (status != null 
            ? query.Where(b => b.Status == status) 
            : query)
            .OrderBy(b => b.OrdemNoBoard)
            .ThenBy(b => b.CreatedAt)
            .ToList();

        // Lista vazia - ordem inicial
        if (orderedBoards.Count == 0)
            return 1000m;

        // Estratégia 1: Por índice de posição
        if (targetIndex.HasValue)
        {
            if (targetIndex.Value <= 0)
            {
                var firstOrder = orderedBoards[0].OrdemNoBoard;
                return firstOrder > 1m ? firstOrder - 1000m : firstOrder / 2m;
            }

            if (targetIndex.Value >= orderedBoards.Count)
                return orderedBoards[^1].OrdemNoBoard + 1000m;

            var prevOrder = orderedBoards[targetIndex.Value - 1].OrdemNoBoard;
            var nextOrder = orderedBoards[targetIndex.Value].OrdemNoBoard;
            return (prevOrder + nextOrder) / 2m;
        }

        // Estratégia 2: Relativa a uma ordem de referência
        if (referenceOrderValue.HasValue)
        {
            if (insertBefore)
            {
                var previousBoard = orderedBoards
                    .Where(b => b.OrdemNoBoard < referenceOrderValue.Value)
                    .OrderByDescending(b => b.OrdemNoBoard)
                    .FirstOrDefault();

                if (previousBoard == null)
                    return referenceOrderValue.Value > 1m ? referenceOrderValue.Value - 1000m : referenceOrderValue.Value / 2m;

                return (previousBoard.OrdemNoBoard + referenceOrderValue.Value) / 2m;
            }
            else
            {
                var nextBoard = orderedBoards
                    .Where(b => b.OrdemNoBoard > referenceOrderValue.Value)
                    .OrderBy(b => b.OrdemNoBoard)
                    .FirstOrDefault();

                if (nextBoard == null)
                    return referenceOrderValue.Value + 1000m;

                return (referenceOrderValue.Value + nextBoard.OrdemNoBoard) / 2m;
            }
        }

        // Estratégia padrão: No final da lista
        return orderedBoards[^1].OrdemNoBoard + 1000m;
    }

    /// <summary>
    /// Rebalanceia as ordens de todas as tarefas quando os valores decimais ficam muito próximos.
    /// </summary>
    private async Task RebalanceOrdersCoreAsync(Guid projectId, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var orderedBoards = (status != null 
            ? query.Where(b => b.Status == status) 
            : query)
            .OrderBy(b => b.OrdemNoBoard)
            .ThenBy(b => b.CreatedAt)
            .ToList();

        decimal currentOrder = 1000m;
        const decimal increment = 1000m;

        foreach (var board in orderedBoards)
        {
            board.UpdateOrdemNoBoard(currentOrder);
            await _boardRepository.UpdateAsync(board, cancellationToken);
            currentOrder += increment;
        }
    }
}
