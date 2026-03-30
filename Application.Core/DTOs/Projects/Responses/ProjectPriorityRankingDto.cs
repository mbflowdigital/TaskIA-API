using Domain.Enums;

namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// DTO para retorno de uma prioridade do projeto com o nome real do enum
/// </summary>
public class ProjectPriorityRankingDto
{
    public Guid Id { get; set; }
    public PriorityType PriorityType { get; set; }
    public string PriorityTypeDescricao { get; set; } = string.Empty;
    public int Posicao { get; set; }
}
