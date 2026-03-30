using Application.Core.DTOs.Projects.Responses;
using Domain.Entities;

namespace Application.Core.DTOs.Projects.Extensions;

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

    /// <summary>
    /// Converte ProjectExecutionSettings para ProjectExecutionSettingsDto,
    /// incluindo as prioridades ordenadas por Posicao com os nomes reais dos enums
    /// </summary>
    public static ProjectExecutionSettingsDto ToDto(
        this ProjectExecutionSettings settings,
        IEnumerable<ProjectPriorityRanking>? priorities = null)
    {
        return new ProjectExecutionSettingsDto
        {
            Id = settings.Id,
            ProjectId = settings.ProjectId,
            ExperienciaEquipe = settings.ExperienciaEquipe,
            ExperienciaEquipeDescricao = settings.ExperienciaEquipe.ToString(),
            NivelDetalhePlano = settings.NivelDetalhePlano,
            NivelDetalhePlanoDescricao = settings.NivelDetalhePlano.ToString(),
            FrequenciaRevisao = settings.FrequenciaRevisao,
            FrequenciaRevisaoDescricao = settings.FrequenciaRevisao.ToString(),
            MaiorRisco = settings.MaiorRisco,
            Observacoes = settings.Observacoes,
            OQueDeuCerto = settings.OQueDeuCerto,
            OQueDeuErrado = settings.OQueDeuErrado,
            PrioridadesOrdenadas = priorities?
                .OrderBy(p => p.Posicao)
                .Select(p => new ProjectPriorityRankingDto
                {
                    Id = p.Id,
                    PriorityType = p.PriorityType,
                    PriorityTypeDescricao = p.PriorityType.ToString(),
                    Posicao = p.Posicao
                })
                .ToList() ?? new List<ProjectPriorityRankingDto>(),
            IsActive = settings.IsActive,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }

    /// <summary>
    /// Converte Project para ProjectCompleteDto com todos os campos enum resolvidos para texto
    /// e todas as coleções ativas ordenadas
    /// </summary>
    public static ProjectCompleteDto ToCompleteDto(this Project project)
    {
        return new ProjectCompleteDto
        {
            Id = project.Id,
            CompanyId = project.CompanyId,
            CompanyName = project.Company?.Name,
            UserId = project.UserId,
            UserName = project.User?.Name,
            Name = project.Name,
            Objective = project.Objective,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ResponsibleSector = project.ResponsibleSector,
            ProjectType = project.ProjectType,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,

            Members = project.ProjectMembers?
                .Where(m => m.IsActive)
                .Select(m => new ProjectMemberCompleteDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User?.Name,
                    ProjectFunction = m.ProjectFunction,
                    Dedication = m.Dedication,
                    Approver = m.Approver,
                    FunctionDescription = m.FunctionDescription,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToList() ?? new List<ProjectMemberCompleteDto>(),

            Details = project.ProjectDetails == null ? null : new ProjectDetailsCompleteDto
            {
                Id = project.ProjectDetails.Id,
                TemDependenciasExternas = project.ProjectDetails.TemDependenciasExternas,
                TemIntegracoes = project.ProjectDetails.TemIntegracoes,
                Orcamento = project.ProjectDetails.Orcamento.ToString(),
                ValorOrcamento = project.ProjectDetails.ValorOrcamento,
                HorarioTrabalho = project.ProjectDetails.HorarioTrabalho.ToString(),
                DowntimePermitido = project.ProjectDetails.DowntimePermitido.ToString(),
                HorasDowntime = project.ProjectDetails.HorasDowntime,
                IsActive = project.ProjectDetails.IsActive,
                CreatedAt = project.ProjectDetails.CreatedAt,
                UpdatedAt = project.ProjectDetails.UpdatedAt,

                Compliances = project.ProjectDetails.Compliances?
                    .Where(c => c.IsActive)
                    .Select(c => new ProjectComplianceCompleteDto
                    {
                        Id = c.Id,
                        TipoCompliance = c.TipoCompliance.ToString(),
                        Observacoes = c.Observacoes,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt
                    })
                    .ToList() ?? new List<ProjectComplianceCompleteDto>(),

                UnavailablePeriods = project.ProjectDetails.UnavailablePeriods?
                    .Where(p => p.IsActive)
                    .Select(p => new ProjectUnavailablePeriodCompleteDto
                    {
                        Id = p.Id,
                        DataInicio = p.DataInicio,
                        DataFim = p.DataFim,
                        Motivo = p.Motivo,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList() ?? new List<ProjectUnavailablePeriodCompleteDto>(),

                Dependencies = project.Dependencies
                    .Where(d => d.IsActive)
                    .Select(d => new ProjectDependencyCompleteDto
                    {
                        Id = d.Id,
                        Nome = d.Nome,
                        Descricao = d.Descricao,
                        Prazo = d.Prazo,
                        Criticidade = d.Criticidade,
                        IsActive = d.IsActive,
                        CreatedAt = d.CreatedAt
                    })
                    .ToList(),

                Integrations = project.Integrations
                    .Where(i => i.IsActive)
                    .Select(i => new ProjectIntegrationCompleteDto
                    {
                        Id = i.Id,
                        NomeSistema = i.NomeSistema,
                        Tipo = i.Tipo,
                        Criticidade = i.Criticidade,
                        Status = i.Status.ToString(),
                        IsActive = i.IsActive,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    })
                    .ToList(),

                SensitiveData = project.SensitiveData
                    .Where(s => s.IsActive)
                    .Select(s => new ProjectSensitiveDataCompleteDto
                    {
                        Id = s.Id,
                        TipoDadoSensivel = s.TipoDadoSensivel.ToString(),
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt
                    })
                    .ToList()
            },

            ExecutionSettings = project.ExecutionSettings == null ? null : new ProjectExecutionSettingsCompleteDto
            {
                Id = project.ExecutionSettings.Id,
                ExperienciaEquipe = project.ExecutionSettings.ExperienciaEquipe.ToString(),
                NivelDetalhePlano = project.ExecutionSettings.NivelDetalhePlano.ToString(),
                FrequenciaRevisao = project.ExecutionSettings.FrequenciaRevisao.ToString(),
                MaiorRisco = project.ExecutionSettings.MaiorRisco,
                Observacoes = project.ExecutionSettings.Observacoes,
                OQueDeuCerto = project.ExecutionSettings.OQueDeuCerto,
                OQueDeuErrado = project.ExecutionSettings.OQueDeuErrado,
                IsActive = project.ExecutionSettings.IsActive,
                CreatedAt = project.ExecutionSettings.CreatedAt,
                UpdatedAt = project.ExecutionSettings.UpdatedAt,

                PrioridadesOrdenadas = project.PriorityRankings
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Posicao)
                    .Select(r => new ProjectPriorityRankingCompleteDto
                    {
                        Id = r.Id,
                        Posicao = r.Posicao,
                        PriorityType = r.PriorityType.ToString()
                    })
                    .ToList()
            }
        };
    }
}

