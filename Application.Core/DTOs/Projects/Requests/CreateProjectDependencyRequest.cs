namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para criação de dependência externa do projeto
/// </summary>
public class CreateProjectDependencyRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Prazo { get; set; }
    public string Criticidade { get; set; } = string.Empty;
}
