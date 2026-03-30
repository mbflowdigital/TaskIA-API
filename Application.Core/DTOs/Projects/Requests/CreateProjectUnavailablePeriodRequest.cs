namespace Application.Core.DTOs.Projects.Requests;

/// <summary>
/// DTO para adicionar período indisponível ao projeto
/// </summary>
public class CreateProjectUnavailablePeriodRequest
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
