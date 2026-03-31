using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Entidade de Tarefa do Projeto
/// Representa uma tarefa gerenciada dentro de um projeto
/// </summary>
public class Board : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "A Fazer"; // A Fazer, Em Andamento, Concluído

    [Required]
    [MaxLength(50)]
    public string Priority { get; set; } = "Média"; // Baixa, Média, Alta, Crítica

    public Guid? SugestaoResponsavelId { get; set; }

    [ForeignKey(nameof(SugestaoResponsavelId))]
    public virtual User? SugestaoResponsavel { get; set; }

    public int PrazoEmDias { get; set; }

    [MaxLength(50)]
    public string? OrdemNoBoard { get; set; }

    // Relacionamento com Project
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    // Relacionamento com User (Responsável pela tarefa)
    public Guid? ResponsavelId { get; set; }

    [ForeignKey(nameof(ResponsavelId))]
    public virtual User? Responsavel { get; set; }

    // Construtor público
    public Board() { }

    /// <summary>
    /// Construtor para criar uma nova tarefa
    /// </summary>
    public Board(
        Guid projectId,
        string name,
        string? description,
        string status,
        string priority,
        Guid? sugestaoResponsavelId,
        int prazoEmDias,
        string? ordemNoBoard)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
        Status = IsValidStatus(status) ? status : "A Fazer";
        Priority = IsValidPriority(priority) ? priority : "Média";
        SugestaoResponsavelId = sugestaoResponsavelId;
        PrazoEmDias = prazoEmDias;
        OrdemNoBoard = ordemNoBoard;
    }

    /// <summary>
    /// Atualiza informações básicas da tarefa
    /// </summary>
    public void UpdateInfo(
        string name,
        string? description,
        Guid? sugestaoResponsavelId,
        int prazoEmDias,
        string? ordemNoBoard)
    {
        Name = name;
        Description = description;
        SugestaoResponsavelId = sugestaoResponsavelId;
        PrazoEmDias = prazoEmDias;
        OrdemNoBoard = ordemNoBoard;
        SetUpdatedAt();
    }

    /// <summary>
    /// Atualiza status da tarefa
    /// </summary>
    public void UpdateStatus(string status)
    {
        if (IsValidStatus(status))
        {
            Status = status;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Atualiza prioridade da tarefa
    /// </summary>
    public void UpdatePriority(string priority)
    {
        if (IsValidPriority(priority))
        {
            Priority = priority;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Atualiza ordem no board
    /// </summary>
    public void UpdateOrdemNoBoard(string ordemNoBoard)
    {
        OrdemNoBoard = ordemNoBoard;
        SetUpdatedAt();
    }

    /// <summary>
    /// Atribui responsável à tarefa
    /// </summary>
    public void AssignResponsavel(Guid? responsavelId)
    {
        ResponsavelId = responsavelId;
        SetUpdatedAt();
    }

    /// <summary>
    /// Remove responsável da tarefa
    /// </summary>
    public void RemoveResponsavel()
    {
        ResponsavelId = null;
        SetUpdatedAt();
    }

    /// <summary>
    /// Atribui sugestão de responsável à tarefa
    /// </summary>
    public void AssignSugestaoResponsavel(Guid? sugestaoResponsavelId)
    {
        SugestaoResponsavelId = sugestaoResponsavelId;
        SetUpdatedAt();
    }

    /// <summary>
    /// Remove sugestão de responsável da tarefa
    /// </summary>
    public void RemoveSugestaoResponsavel()
    {
        SugestaoResponsavelId = null;
        SetUpdatedAt();
    }

    /// <summary>
    /// Valida se o status é válido
    /// </summary>
    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "A Fazer", "Em Andamento", "Concluído" };
        return validStatuses.Contains(status);
    }

    /// <summary>
    /// Valida se a prioridade é válida
    /// </summary>
    private static bool IsValidPriority(string priority)
    {
        var validPriorities = new[] { "Baixa", "Média", "Alta", "Crítica" };
        return validPriorities.Contains(priority);
    }

    /// <summary>
    /// Soft delete da tarefa
    /// </summary>
    public void SoftDelete()
    {
        Deactivate();
        SetUpdatedAt();
    }
}
