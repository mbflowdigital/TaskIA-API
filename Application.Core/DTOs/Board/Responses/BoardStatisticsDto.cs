namespace Application.Core.DTOs.Board.Responses;

/// <summary>
/// DTO para estatísticas de tarefas do projeto
/// </summary>
public record BoardStatisticsDto(
    int TotalTasks,
    int TasksAFazer,
    int TasksEmAndamento,
    int TasksConcluidas,
    int TasksBaixaPrioridade,
    int TasksMediaPrioridade,
    int TasksAltaPrioridade,
    int TasksCriticaPrioridade
);
