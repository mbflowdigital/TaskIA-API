namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para atribuir/alterar sugestão de responsável
/// </summary>
public record AssignSugestaoResponsavelRequest(
    Guid? SugestaoResponsavelId
);
