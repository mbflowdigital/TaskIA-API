using Domain.Entities;

namespace Application.Core.DTOs.Projects;

/// <summary>
/// Métodos de extensão para mapeamento de ProjectDetails e entidades relacionadas
/// </summary>
public static class ProjectDetailsDtoExtensions
{
    /// <summary>
    /// Converte ProjectDetails para ProjectDetailsDto, incluindo opcionalmente as coleções do Project
    /// </summary>
    public static ProjectDetailsDto ToDto(
        this ProjectDetails details,
        IEnumerable<ProjectDependencies>? dependencies = null,
        IEnumerable<ProjectIntegrations>? integrations = null,
        IEnumerable<ProjectSensitiveData>? sensitiveData = null)
    {
        return new ProjectDetailsDto
        {
            Id = details.Id,
            ProjectId = details.ProjectId,
            TemDependenciasExternas = details.TemDependenciasExternas,
            TemIntegracoes = details.TemIntegracoes,
            Orcamento = details.Orcamento,
            OrcamentoDescricao = details.Orcamento.ToString(),
            ValorOrcamento = details.ValorOrcamento,
            HorarioTrabalho = details.HorarioTrabalho,
            HorarioTrabalhoDescricao = details.HorarioTrabalho.ToString(),
            DowntimePermitido = details.DowntimePermitido,
            DowntimePermitidoDescricao = details.DowntimePermitido.ToString(),
            HorasDowntime = details.HorasDowntime,
            Compliances = details.Compliances?
                .Where(c => c.IsActive)
                .Select(c => c.ToDto())
                .ToList() ?? new List<ProjectComplianceDto>(),
            UnavailablePeriods = details.UnavailablePeriods?
                .Where(p => p.IsActive)
                .Select(p => p.ToDto())
                .ToList() ?? new List<ProjectUnavailablePeriodDto>(),
            Dependencies = dependencies?
                .Select(d => d.ToDto())
                .ToList() ?? new List<ProjectDependencyDto>(),
            Integrations = integrations?
                .Select(i => i.ToDto())
                .ToList() ?? new List<ProjectIntegrationDto>(),
            SensitiveData = sensitiveData?
                .Select(s => s.ToDto())
                .ToList() ?? new List<ProjectSensitiveDataDto>(),
            IsActive = details.IsActive,
            CreatedAt = details.CreatedAt,
            UpdatedAt = details.UpdatedAt
        };
    }

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

    public static ProjectDependencyDto ToDto(this ProjectDependencies dependency)
    {
        return new ProjectDependencyDto
        {
            Id = dependency.Id,
            ProjectId = dependency.ProjectId,
            Nome = dependency.Nome,
            Descricao = dependency.Descricao,
            Prazo = dependency.Prazo,
            Criticidade = dependency.Criticidade,
            IsActive = dependency.IsActive,
            CreatedAt = dependency.CreatedAt,
            UpdatedAt = dependency.UpdatedAt
        };
    }

    public static ProjectIntegrationDto ToDto(this ProjectIntegrations integration)
    {
        return new ProjectIntegrationDto
        {
            Id = integration.Id,
            ProjectId = integration.ProjectId,
            NomeSistema = integration.NomeSistema,
            Tipo = integration.Tipo,
            Criticidade = integration.Criticidade,
            Status = integration.Status,
            StatusDescricao = integration.Status.ToString(),
            IsActive = integration.IsActive,
            CreatedAt = integration.CreatedAt,
            UpdatedAt = integration.UpdatedAt
        };
    }

    public static ProjectSensitiveDataDto ToDto(this ProjectSensitiveData data)
    {
        return new ProjectSensitiveDataDto
        {
            Id = data.Id,
            ProjectId = data.ProjectId,
            TipoDadoSensivel = data.TipoDadoSensivel,
            TipoDadoSensivelDescricao = data.TipoDadoSensivel.ToString(),
            IsActive = data.IsActive,
            CreatedAt = data.CreatedAt
        };
    }
}

