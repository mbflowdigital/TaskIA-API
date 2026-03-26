using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Ranking de prioridades do projeto.
/// Relação 1:N com Project — cada registro representa uma prioridade com sua posição de ordenação.
/// </summary>
public class ProjectPriorityRanking : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [Required]
    public PriorityType PriorityType { get; set; }

    [Required]
    public int Posicao { get; set; }

    public ProjectPriorityRanking() { }

    /// <summary>
    /// Atualiza a posição da prioridade no ranking
    /// </summary>
    public void UpdatePosicao(int posicao)
    {
        Posicao = posicao;
        SetUpdatedAt();
    }
}
