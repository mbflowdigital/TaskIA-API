using Domain.Entities;

namespace Application.Core.DTOs.Projects;

/// <summary>
/// Métodos de extensão para mapeamento de ProjectDetails
/// </summary>
public static class ProjectDetailsDtoExtensions
{
    /// <summary>
    /// Converte ProjectDetails para ProjectDetailsDto
    /// </summary>
    public static ProjectDetailsDto ToDto(this ProjectDetails details)
    {
        return new ProjectDetailsDto
        {
            Id = details.Id,
            ProjectId = details.ProjectId,
            TemDependenciasExternas = details.TemDependenciasExternas,
            TemIntegracoes = details.TemIntegracoes,
            Orcamento = details.Orcamento,
            OrcamentoDescricao = details.Orcamento.ToString(),
            HorarioTrabalho = details.HorarioTrabalho,
            HorarioTrabalhoDescricao = details.HorarioTrabalho.ToString(),
            DowntimePermitido = details.DowntimePermitido,
            DowntimePermitidoDescricao = details.DowntimePermitido.ToString(),
            Compliances = details.Compliances?
                .Where(c => c.IsActive)
                .Select(c => c.ToDto())
                .ToList() ?? new List<ProjectComplianceDto>(),
            UnavailablePeriods = details.UnavailablePeriods?
                .Where(p => p.IsActive)
                .Select(p => p.ToDto())
                .ToList() ?? new List<ProjectUnavailablePeriodDto>(),
            IsActive = details.IsActive,
            CreatedAt = details.CreatedAt,
            UpdatedAt = details.UpdatedAt
        };
    }

    /// <summary>
    /// Converte ProjectCompliance para ProjectComplianceDto
    /// </summary>
    public static ProjectComplianceDto ToDto(this ProjectCompliance compliance)
    {
        return new ProjectComplianceDto
        {
            Id = compliance.Id,
            TipoCompliance = compliance.TipoCompliance,
            TipoComplianceDescricao = compliance.TipoCompliance.ToString(),
            Observacoes = compliance.Observacoes,
            IsActive = compliance.IsActive,
            CreatedAt = compliance.CreatedAt
        };
    }

    /// <summary>
    /// Converte ProjectUnavailablePeriod para ProjectUnavailablePeriodDto
    /// </summary>
    public static ProjectUnavailablePeriodDto ToDto(this ProjectUnavailablePeriod period)
    {
        return new ProjectUnavailablePeriodDto
        {
            Id = period.Id,
            DataInicio = period.DataInicio,
            DataFim = period.DataFim,
            Motivo = period.Motivo,
            IsPeriodActive = period.IsPeriodActive(),
            IsActive = period.IsActive,
            CreatedAt = period.CreatedAt
        };
    }
}
