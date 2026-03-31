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
    private readonly IBoardDependencyRepository _dependencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BoardService(
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IBoardDependencyRepository dependencyRepository,
        IUnitOfWork unitOfWork)
    {
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _dependencyRepository = dependencyRepository;
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

        // Validar responsável se fornecido
        if (request.ResponsavelId.HasValue)
        {
            var responsavel = await _userRepository.GetByIdAsync(request.ResponsavelId.Value, cancellationToken);
            if (responsavel == null)
                return Result<BoardDto>.Failure("Responsável não encontrado.");
        }

        // Validar sugestão de responsável se fornecido
        if (request.SugestaoResponsavelId.HasValue)
        {
            var sugestao = await _userRepository.GetByIdAsync(request.SugestaoResponsavelId.Value, cancellationToken);
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
        await _unitOfWork.CommitAsync(cancellationToken);

        var createdBoard = await _boardRepository.GetByIdWithProjectAsync(board.Id, cancellationToken);
        var boardDto = MapToDto(createdBoard!);

        return Result<BoardDto>.Success(boardDto, "Tarefa criada com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        if (!IsValidPriority(request.Priority))
            return Result<BoardDto>.Failure("Prioridade inválida. Valores aceitos: Baixa, Média, Alta, Crítica");

        board.UpdateInfo(
            name: request.Name,
            description: request.Description,
            sugestaoResponsavelId: board.SugestaoResponsavelId,
            prazoEmDias: request.PrazoEmDias,
            ordemNoBoard: request.OrdemNoBoard
        );

        board.UpdatePriority(request.Priority);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        var boardDto = MapToDto(updatedBoard!);

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
        var boardDto = MapToDto(updatedBoard!);

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
        var boardDto = MapToDto(updatedBoard!);

        return Result<BoardDto>.Success(boardDto, "Sugestão de responsável atribuída com sucesso.");
    }

    public async Task<Result<BoardDto>> UpdateStatusAsync(Guid id, UpdateBoardStatusRequest request, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, cancellationToken);
        if (board == null)
            return Result<BoardDto>.Failure("Tarefa não encontrada.");

        if (!IsValidStatus(request.Status))
            return Result<BoardDto>.Failure("Status inválido. Valores aceitos: A Fazer, Em Andamento, Concluído");

        // Validação de dependências: só permite iniciar se não houver bloqueios
        if (request.Status == "Em Andamento" && board.Status == "A Fazer")
        {
            var dependencies = await _dependencyRepository.GetDependenciesAsync(id, cancellationToken);
            var blockingDeps = dependencies.Where(d => d.IsBlocking()).ToList();

            if (blockingDeps.Any())
            {
                var blockingNames = string.Join(", ", blockingDeps.Select(d => d.DependsOnBoard.Name));
                return Result<BoardDto>.Failure(
                    $"Não é possível iniciar esta tarefa. Ela depende de {blockingDeps.Count} tarefa(s) não concluída(s): {blockingNames}");
            }
        }

        // Permitir voltar para "A Fazer" de qualquer status
        // Permitir avançar se não houver bloqueios

        board.UpdateStatus(request.Status);

        await _boardRepository.UpdateAsync(board, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var updatedBoard = await _boardRepository.GetByIdWithProjectAsync(id, cancellationToken);
        var boardDto = MapToDto(updatedBoard!);

        return Result<BoardDto>.Success(boardDto, "Status atualizado com sucesso.");
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

    // Métodos de gerenciamento de dependências

    public async Task<Result> AddDependencyAsync(Guid boardId, AddDependencyRequest request, CancellationToken cancellationToken = default)
    {
        // Validar se a tarefa existe
        var board = await _boardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
            return Result.Failure("Tarefa não encontrada.");

        // Validar se a tarefa da dependência existe
        var dependsOnBoard = await _boardRepository.GetByIdAsync(request.DependsOnBoardId, cancellationToken);
        if (dependsOnBoard == null)
            return Result.Failure("Tarefa de dependência não encontrada.");

        // Não permitir que uma tarefa dependa dela mesma
        if (boardId == request.DependsOnBoardId)
            return Result.Failure("Uma tarefa não pode depender dela mesma.");

        // Verificar se as tarefas são do mesmo projeto
        if (board.ProjectId != dependsOnBoard.ProjectId)
            return Result.Failure("As tarefas devem pertencer ao mesmo projeto.");

        // Verificar se a dependência já existe
        var exists = await _dependencyRepository.ExistsAsync(boardId, request.DependsOnBoardId, cancellationToken);
        if (exists)
            return Result.Failure("Esta dependência já existe.");

        // Verificar se criar esta dependência causaria um ciclo
        var wouldCreateCycle = await _dependencyRepository.WouldCreateCycleAsync(boardId, request.DependsOnBoardId, cancellationToken);
        if (wouldCreateCycle)
            return Result.Failure("Não é possível criar esta dependência pois causaria uma dependência circular.");

        // Criar a dependência
        var dependency = new Domain.Entities.BoardDependency(boardId, request.DependsOnBoardId);
        await _dependencyRepository.AddAsync(dependency, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success("Dependência adicionada com sucesso.");
    }

    public async Task<Result> RemoveDependencyAsync(Guid boardId, Guid dependsOnBoardId, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
            return Result.Failure("Tarefa não encontrada.");

        var removed = await _dependencyRepository.RemoveDependencyAsync(boardId, dependsOnBoardId, cancellationToken);
        if (!removed)
            return Result.Failure("Dependência não encontrada.");

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success("Dependência removida com sucesso.");
    }

    public async Task<Result<IEnumerable<BoardDependencyDto>>> GetDependenciesAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
            return Result<IEnumerable<BoardDependencyDto>>.Failure("Tarefa não encontrada.");

        var dependencies = await _dependencyRepository.GetDependenciesAsync(boardId, cancellationToken);
        var dtos = dependencies.Select(d => new BoardDependencyDto(
            Id: d.Id,
            BoardId: d.BoardId,
            DependsOnBoardId: d.DependsOnBoardId,
            DependsOnBoardName: d.DependsOnBoard.Name,
            DependsOnBoardStatus: d.DependsOnBoard.Status,
            IsBlocking: d.IsBlocking()
        )).ToList();

        return Result<IEnumerable<BoardDependencyDto>>.Success(dtos);
    }

    public async Task<Result<BoardBlockingInfoDto>> GetBlockingInfoAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var board = await _boardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
            return Result<BoardBlockingInfoDto>.Failure("Tarefa não encontrada.");

        var dependencies = await _dependencyRepository.GetDependenciesAsync(boardId, cancellationToken);
        var blockingDeps = dependencies.Where(d => d.IsBlocking()).ToList();

        var blockingDtos = blockingDeps.Select(d => new BoardDependencyDto(
            Id: d.Id,
            BoardId: d.BoardId,
            DependsOnBoardId: d.DependsOnBoardId,
            DependsOnBoardName: d.DependsOnBoard.Name,
            DependsOnBoardStatus: d.DependsOnBoard.Status,
            IsBlocking: true
        )).ToList();

        var blockingInfo = new BoardBlockingInfoDto(
            IsBlocked: blockingDeps.Any(),
            BlockingDependenciesCount: blockingDeps.Count,
            BlockingDependencies: blockingDtos
        );

        return Result<BoardBlockingInfoDto>.Success(blockingInfo);
    }
}
