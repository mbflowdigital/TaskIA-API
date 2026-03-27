using Domain.Enums;

namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de uma entrada no ranking de prioridades do projeto
/// </summary>
public class CreateProjectPriorityRankingRequest
{
    public PriorityType PriorityType { get; set; }
    public int Posicao { get; set; }
}
