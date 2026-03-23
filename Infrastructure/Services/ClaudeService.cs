using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Common;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public record ProjectSuggestion(string Description, string Objective);
public record ProjectAnalysis(string Overview, string Risks, string Recommendations);
public record TeamMemberInput(string UserId, string UserName, string Role, string Dedication, bool IsApprover, string? RoleDescription);
public record ExternalDependencyInput(string Name, string WhatIsNeeded, string? Deadline, string Criticality);
public record IntegrationInput(string SystemName, string Type, string Criticality, string Status);
public record UnavailablePeriodInput(string StartDate, string EndDate, string? Reason);
public record ProjectAnalysisInput(
    string ProjectName,
    string Objective,
    string StartDate,
    string? EndDate,
    string? Description,
    string Company,
    string Department,
    string ProjectType,
    List<TeamMemberInput> TeamMembers,
    string? HasExternalDependencies,
    List<ExternalDependencyInput>? ExternalDependencies,
    string? BudgetType,
    string? BudgetValue,
    string? WorkSchedule,
    string? DowntimePolicy,
    string? DowntimeLimitHours,
    string? HasIntegrations,
    List<IntegrationInput>? Integrations,
    List<string>? Compliance,
    List<string>? ComplianceApprovers,
    List<UnavailablePeriodInput>? UnavailablePeriods,
    // Step 4
    List<string>? PriorityRanking,
    string? BiggestRisk,
    string? PreviousExperience,
    string? WhatWentWell,
    string? WhatWentWrong,
    string? DetailLevel,
    string? ReviewFrequency,
    string? FinalObservations
);

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public ClaudeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var rawKey = configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey não configurado.");
        _apiKey = AesProtector.Decrypt(rawKey);
        _model = configuration["Claude:Model"] ?? "claude-sonnet-4-5";
        _baseUrl = configuration["Claude:BaseUrl"] ?? "https://api.anthropic.com/v1";
    }

    public async Task<Result<ProjectSuggestion>> SuggestProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return Result<ProjectSuggestion>.Failure("O nome do projeto é obrigatório.");

        var prompt = BuildPrompt(projectName);

        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<ProjectSuggestion>.Failure($"Erro ao comunicar com Claude: {ex.Message}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<ProjectSuggestion>.Failure($"Claude API retornou erro {(int)response.StatusCode}.");

        try
        {
            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = claudeResponse?.Content?.FirstOrDefault()?.Text ?? string.Empty;
            var suggestion = ParseSuggestion(text);
            return Result<ProjectSuggestion>.Success(suggestion, "Sugestão gerada com sucesso.");
        }
        catch
        {
            return Result<ProjectSuggestion>.Failure("Não foi possível processar a resposta do Claude.");
        }
    }

    private static string BuildPrompt(string projectName)
    {
        return $$"""
                Você é um assistente de gerenciamento de projetos. Com base no nome do projeto abaixo, 
                gere uma descrição e um objetivo claros, objetivos e profissionais em português brasileiro.

                Nome do projeto: {{projectName}}

                Responda SOMENTE no seguinte formato JSON (sem markdown, sem explicações adicionais):
                {
                  "description": "Descrição resumida do projeto em 1 a 2 frases.",
                  "objective": "Objetivo principal do projeto em 1 frase clara e mensurável."
                }
                """;
    }

    private static ProjectSuggestion ParseSuggestion(string text)
    {
        text = text.Trim();

        // Remove possível bloco markdown ```json ... ```
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text[start..(end + 1)];
        }

        var parsed = JsonSerializer.Deserialize<ClaudeSuggestionJson>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new ProjectSuggestion(
            parsed?.Description ?? string.Empty,
            parsed?.Objective ?? string.Empty
        );
    }

    // DTO internos para desserialização da resposta do Claude
    private sealed class ClaudeApiResponse
    {
        [JsonPropertyName("content")]
        public List<ClaudeContentBlock>? Content { get; set; }
    }

    private sealed class ClaudeContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class ClaudeSuggestionJson
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("objective")]
        public string? Objective { get; set; }
    }

    private sealed class ClaudeAnalysisJson
    {
        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("risks")]
        public string? Risks { get; set; }

        [JsonPropertyName("recommendations")]
        public string? Recommendations { get; set; }
    }

    public async Task<Result<ProjectAnalysis>> AnalyzeProjectAsync(ProjectAnalysisInput data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(data?.ProjectName))
            return Result<ProjectAnalysis>.Failure("O nome do projeto é obrigatório.");

        var prompt = BuildAnalysisPrompt(data);

        var requestBody = new
        {
            model = _model,
            max_tokens = 2048,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<ProjectAnalysis>.Failure($"Erro ao comunicar com Claude: {ex.Message}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<ProjectAnalysis>.Failure($"Claude API retornou erro {(int)response.StatusCode}.");

        try
        {
            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = claudeResponse?.Content?.FirstOrDefault()?.Text ?? string.Empty;
            var analysis = ParseAnalysis(text);
            return Result<ProjectAnalysis>.Success(analysis, "Análise gerada com sucesso.");
        }
        catch
        {
            return Result<ProjectAnalysis>.Failure("Não foi possível processar a resposta do Claude.");
        }
    }

    private static string BuildAnalysisPrompt(ProjectAnalysisInput data)
    {
        var endDateText = string.IsNullOrWhiteSpace(data.EndDate) ? "Não definida" : data.EndDate;
        var descriptionText = string.IsNullOrWhiteSpace(data.Description) ? "Não informada" : data.Description;
        var membersText = data.TeamMembers.Count == 0
            ? "Nenhum membro selecionado."
            : string.Join("\n", data.TeamMembers.Select(m =>
                $"  - {m.UserName}: {m.Role}, {m.Dedication}{(m.IsApprover ? " (Aprovador)" : "")}{(string.IsNullOrWhiteSpace(m.RoleDescription) ? "" : $" | Descrição: {m.RoleDescription}")}"));

        var depsText = data.HasExternalDependencies == "yes" && data.ExternalDependencies?.Count > 0
            ? string.Join("\n", data.ExternalDependencies.Select(d =>
                $"  - {d.Name}: {d.WhatIsNeeded}, Criticidade: {d.Criticality}{(string.IsNullOrWhiteSpace(d.Deadline) ? "" : $", Prazo: {d.Deadline}")}"))
            : "Nenhuma";

        var budgetText = data.BudgetType switch
        {
            "fixed" => $"Valor fixo: R$ {data.BudgetValue}",
            "tbd" => "A definir",
            _ => "Sem limite"
        };

        var workText = data.WorkSchedule switch
        {
            "flexible" => "Flexível",
            "off-hours" => "Fora do expediente",
            _ => "Comercial"
        };

        var downtimeText = data.DowntimePolicy switch
        {
            "limited" => $"Até {data.DowntimeLimitHours ?? "?"} horas",
            "zero" => "Zero downtime",
            _ => "Não se aplica"
        };

        var intgText = data.HasIntegrations == "yes" && data.Integrations?.Count > 0
            ? string.Join("\n", data.Integrations.Select(i =>
                $"  - {i.SystemName} ({i.Type}), Criticidade: {i.Criticality}, Status: {(i.Status == "to-create" ? "A criar" : "Já existe")}"))
            : "Nenhuma";

        var complianceText = data.Compliance?.Count > 0
            ? string.Join(", ", data.Compliance)
            : "Nenhum";

        var approversText = data.ComplianceApprovers?.Count > 0
            ? string.Join(", ", data.ComplianceApprovers)
            : "Nenhum";

        var periodsText = data.UnavailablePeriods?.Count > 0
            ? string.Join("\n", data.UnavailablePeriods.Select(p =>
                $"  - {p.StartDate} a {p.EndDate}{(string.IsNullOrWhiteSpace(p.Reason) ? "" : $": {p.Reason}")}"))
            : "Nenhum";

        var prioritiesText = data.PriorityRanking?.Count > 0
            ? string.Join(", ", data.PriorityRanking.Select((p, i) => $"{i + 1}. {p}"))
            : "Não informado";

        var experienceText = data.PreviousExperience switch
        {
            "similar" => "Algo similar",
            "exact" => "Exatamente isso",
            _ => "Nunca fizemos"
        };

        var detailText = data.DetailLevel switch
        {
            "macro" => "Macro (10–15 tarefas)",
            "granular" => "Granular (80–150 tarefas)",
            _ => "Balanceado (30–50 tarefas)"
        };

        var reviewText = data.ReviewFrequency switch
        {
            "biweekly" => "Quinzenal",
            "monthly" => "Mensal",
            _ => "Semanal"
        };

        var biggestRiskText = string.IsNullOrWhiteSpace(data.BiggestRisk) ? "Não informado" : data.BiggestRisk;
        var observationsText = string.IsNullOrWhiteSpace(data.FinalObservations) ? "Nenhuma" : data.FinalObservations;

        return $$"""
            Você é um consultor especialista em gerenciamento de projetos. Analise o projeto abaixo e forneça uma análise detalhada em português brasileiro.

            === DADOS DO PROJETO ===
            Nome: {{data.ProjectName}}
            Objetivo: {{data.Objective}}
            Data de início: {{data.StartDate}}
            Data de término: {{endDateText}}
            Descrição: {{descriptionText}}
            Empresa: {{data.Company}}
            Departamento: {{data.Department}}
            Tipo de projeto: {{data.ProjectType}}

            === EQUIPE ===
            {{membersText}}

            === CONTEXTO E RESTRIÇÕES ===
            Dependências externas:
            {{depsText}}
            Orçamento: {{budgetText}}
            Horário de trabalho: {{workText}}
            Downtime permitido: {{downtimeText}}
            Integrações:
            {{intgText}}
            Compliance: {{complianceText}}
            Aprovadores de compliance: {{approversText}}
            Períodos indisponíveis:
            {{periodsText}}

            === PRIORIDADES E EXPECTATIVAS ===
            Ranking de prioridades (1 = mais importante): {{prioritiesText}}
            Maior risco percebido: {{biggestRiskText}}
            Experiência prévia: {{experienceText}}
            O que deu certo em projetos anteriores: {{(string.IsNullOrWhiteSpace(data.WhatWentWell) ? "N/A" : data.WhatWentWell)}}
            O que deu errado em projetos anteriores: {{(string.IsNullOrWhiteSpace(data.WhatWentWrong) ? "N/A" : data.WhatWentWrong)}}
            Nível de detalhe desejado: {{detailText}}
            Frequência de revisão: {{reviewText}}
            Observações finais: {{observationsText}}

            Responda SOMENTE no seguinte formato JSON (sem markdown, sem explicações adicionais):
            {
              "overview": "Análise geral do projeto: viabilidade, pontos fortes e contexto em 2-3 frases.",
              "risks": "Principais riscos identificados, separados por ponto e vírgula.",
              "recommendations": "Recomendações para o sucesso do projeto, separadas por ponto e vírgula."
            }
            """;
    }

    private static ProjectAnalysis ParseAnalysis(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text[start..(end + 1)];
        }

        var parsed = JsonSerializer.Deserialize<ClaudeAnalysisJson>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new ProjectAnalysis(
            parsed?.Overview ?? string.Empty,
            parsed?.Risks ?? string.Empty,
            parsed?.Recommendations ?? string.Empty
        );
    }
}
