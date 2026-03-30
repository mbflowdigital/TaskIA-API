namespace Application.Core.DTOs.Projects.Responses;

/// <summary>
/// Prioridade do projeto na visão completa — PriorityType como texto, ordenado por Posicao
/// </summary>
public class ProjectPriorityRankingCompleteDto
{
    public Guid Id { get; set; }
    public int Posicao { get; set; }
    public string PriorityType { get; set; } = string.Empty;
}
