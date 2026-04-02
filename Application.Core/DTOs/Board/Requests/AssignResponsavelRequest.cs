namespace Application.Core.DTOs.Board.Requests;

/// <summary>
/// Request para atribuir/alterar responsável
/// </summary>
public record AssignResponsavelRequest(
    Guid? ResponsavelId
);
