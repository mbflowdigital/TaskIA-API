using Application.Core.DTOs.Board.Responses;
using Domain.Entities;

namespace Application.Core.DTOs.Board.Extensions;

/// <summary>
/// Métodos de extensão para mapeamento de Board (Tarefas)
/// </summary>
public static class BoardDtoExtensions
{
    /// <summary>
    /// Converte uma entidade Board para BoardDto
    /// </summary>
    /// <param name="board">Entidade Board a ser convertida</param>
    /// <returns>BoardDto com os dados mapeados</returns>
    public static BoardDto ToDto(this Domain.Entities.Board board)
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

    /// <summary>
    /// Converte uma coleção de Board para uma coleção de BoardDto
    /// </summary>
    /// <param name="boards">Coleção de entidades Board</param>
    /// <returns>Coleção de BoardDto</returns>
    public static IEnumerable<BoardDto> ToDto(this IEnumerable<Domain.Entities.Board> boards)
    {
        return boards.Select(b => b.ToDto());
    }
}
