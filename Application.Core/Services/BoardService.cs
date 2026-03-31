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
        var boardDtos = boards.Select(MapToDto).ToList();

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
        var boardDtos = boards.Select(MapToDto).ToList();

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
        var boardDtos = boards.Select(MapToDto).ToList();

        return Result<IEnumerable<BoardDto>>.Success(boardDtos);
    }

    public async Task<Result<BoardDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        var boardDto = MapToDto(board);
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

        var board = new Board(
            projectId: request.ProjectId,
            name: request.Name,
            description: request.Description,
            status: request.Status,
            priority: request.Priority,
            sugestaoResponsavelId: request.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: request.OrdemNoBoard
        );

        if (request.ResponsavelId.HasValue)
        {
            board.AssignResponsavel(request.ResponsavelId.Value);
        }

        await _boardRepository.AddAsync(board, cancellationToken);

        // TODO: Implementar lógica de reorganização automática de ordens
        // Quando uma tarefa é criada com uma ordem específica, as outras tarefas
        // com ordem >= devem ser incrementadas automaticamente

        await _unitOfWork.CommitAsync(cancellationToken);

        // Carregar relacionamentos manualmente para evitar segunda query
        board.Project = project;
        if (responsavel != null)
            board.Responsavel = responsavel;
        if (sugestao != null)
            board.SugestaoResponsavel = sugestao;

        var boardDto = MapToDto(board);

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

        board.UpdateInfo(
            name: request.Name,
            description: request.Description,
            sugestaoResponsavelId: board.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: request.OrdemNoBoard
        );

        board.UpdatePriority(request.Priority);

        await _boardRepository.UpdateAsync(board, cancellationToken);

        // TODO: Implementar lógica de reorganização automática de ordens
        // Quando a ordem é alterada, as outras tarefas devem ser reorganizadas

        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (updatedBoard == null)
            return Result<BoardDto>.Failure("Erro ao recuperar a tarefa atualizada.");

        var boardDto = MapToDto(updatedBoard);

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

        var boardDto = MapToDto(updatedBoard);

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

        var boardDto = MapToDto(updatedBoard);

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

        var boardDto = MapToDto(updatedBoard);

        return Result<BoardDto>.Success(boardDto, "Status atualizado com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateOrdemAsync(Guid id, UpdateBoardOrdemRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        // Validar se a ordem é válida
        if (string.IsNullOrWhiteSpace(request.OrdemNoBoard))
            return Result<BoardDto>.Failure("Ordem não pode ser vazia.");

        // Atualizar a ordem da tarefa
        board.UpdateOrdemNoBoard(request.OrdemNoBoard);
        await _boardRepository.UpdateAsync(board, cancellationToken);

        // TODO: Implementar lógica de reorganização automática de ordens
        // Quando a ordem é alterada, as outras tarefas com ordem >= devem ser incrementadas
        // Exemplo: Se mover para posição 2, as tarefas 2, 3, 4... devem virar 3, 4, 5...

        await _unitOfWork.CommitAsync(cancellationToken);

        var boardDto = MapToDto(board);

        return Result<BoardDto>.Success(boardDto, "Ordem atualizada com sucesso.");
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
    private static BoardDto MapToDto(Board board)
    {
        return new BoardDto(
            Id: board.Id,
            ProjectId: board.ProjectId,
            ProjectName: board.Project?.Name ?? string.Empty,
            Name: board.Name,
            Description: board.Description,
            Status: board.Status,
            Priority: board.Priority,
            PrazoEmDias: board.PrazoEmDias,
            OrdemNoBoard: board.OrdemNoBoard,
            ResponsavelId: board.ResponsavelId,
            ResponsavelName: board.Responsavel?.Name,
            SugestaoResponsavelId: board.SugestaoResponsavelId,
            SugestaoResponsavelName: board.SugestaoResponsavel?.Name,
            CreatedAt: board.CreatedAt,
            UpdatedAt: board.UpdatedAt
        );
    }

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

    // TODO: Implementar reorganização automática de ordens
    // Este método será usado para reorganizar automaticamente as ordens das tarefas
    // quando uma tarefa é inserida ou movida para uma posição específica
    /*
    /// <summary>
    /// Reorganiza as ordens de todas as tarefas do projeto quando uma tarefa é inserida/movida para uma posição específica
    /// </summary>
    private async Task ReorganizarOrdensAsync(Guid projectId, Guid boardId, string? novaOrdem, CancellationToken cancellationToken)
    {
        // Se não foi fornecida uma ordem, não faz nada
        if (string.IsNullOrWhiteSpace(novaOrdem))
            return;

        // Validar se a ordem é um número válido
        if (!int.TryParse(novaOrdem, out int ordemDesejada) || ordemDesejada < 1)
            return;

        // Buscar todas as tarefas do projeto (exceto a que está sendo alterada)
        var allBoards = (await _boardRepository.GetByProjectIdAsync(projectId, cancellationToken))
            .Where(b => b.Id != boardId)
            .OrderBy(b => {
                if (int.TryParse(b.OrdemNoBoard, out int ordem))
                    return ordem;
                return 999999;
            })
            .ThenBy(b => b.CreatedAt)
            .ToList();

        // Incrementar a ordem de todas as tarefas que estão na posição desejada ou depois
        foreach (var board in allBoards)
        {
            if (int.TryParse(board.OrdemNoBoard, out int ordemAtual) && ordemAtual >= ordemDesejada)
            {
                var novaOrdemBoard = (ordemAtual + 1).ToString();
                board.UpdateOrdemNoBoard(novaOrdemBoard);
                await _boardRepository.UpdateAsync(board, cancellationToken);
            }
        }
    }
    */
}
