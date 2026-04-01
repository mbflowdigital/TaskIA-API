namespace Application.Core.DTOs.Board.Responses;

/// <summary>
/// DTO de resposta para Board/Tarefa
/// </summary>
public record BoardDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Name,
    string? Description,
    string Status,
    string Priority,
    int PrazoEmDias,
    decimal OrdemNoBoard,
    Guid? ResponsavelId,
    string? ResponsavelName,
    Guid? SugestaoResponsavelId,
    string? SugestaoResponsavelName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
