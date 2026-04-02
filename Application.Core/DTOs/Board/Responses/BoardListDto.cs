namespace Application.Core.DTOs.Board.Responses;

/// <summary>
/// DTO simplificado para listagem de tarefas
/// </summary>
public record BoardListDto(
    Guid Id,
    string Name,
    string Status,
    string Priority,
    int PrazoEmDias,
    string? OrdemNoBoard,
    string? ResponsavelName,
    string? SugestaoResponsavelName
);
