using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Common;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public record ProjectSuggestion(string Description, string Objective);
public record TaskSuggestion(string Name, string? Description, string Priority, string? SuggestedResponsible, int DeadlineInDays, decimal Order);
public record ProjectAnalysis(string Overview, string Risks, string Recommendations, List<TaskSuggestion> Tasks, string PromptSent);
public record TeamMemberInput(string UserId, string UserName, string Role, string Dedication, bool IsApprover, string? RoleDescription);
public record ExternalDependencyInput(string Name, string WhatIsNeeded, string? Deadline, string Criticality);
public record IntegrationInput(string SystemName, string Type, string Criticality, string Status);
public record UnavailablePeriodInput(string StartDate, string EndDate, string? Reason);
public record ProjectAnalysisInput(
    Guid ProjectId,
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
    private readonly IParameterRepository _parameterRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly ApplicationDbContext _context;

    public ClaudeService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        IParameterRepository parameterRepository, 
        IProjectRepository projectRepository, 
        IBoardRepository boardRepository,
        ApplicationDbContext context)
    {
        _httpClient = httpClient;
        var rawKey = configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey não configurado.");
        _apiKey = AesProtector.Decrypt(rawKey);
        _model = configuration["Claude:Model"] ?? "claude-sonnet-4-5";
        _baseUrl = configuration["Claude:BaseUrl"] ?? "https://api.anthropic.com/v1";
        _parameterRepository = parameterRepository;
        _projectRepository = projectRepository;
        _boardRepository = boardRepository;
        _context = context;
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

        [JsonPropertyName("tasks")]
        public List<ClaudeTaskJson>? Tasks { get; set; }
    }

    private sealed class ClaudeTaskJson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("suggestedResponsible")]
        public string? SuggestedResponsible { get; set; }

        [JsonPropertyName("deadlineInDays")]
        public int DeadlineInDays { get; set; }

        [JsonPropertyName("order")]
        public decimal Order { get; set; }
    }

    public async Task<Result<ProjectAnalysis>> AnalyzeProjectAsync(
        ProjectAnalysisInput data, 
        string? additionalDocumentContext = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(data?.ProjectName))
            return Result<ProjectAnalysis>.Failure("O nome do projeto é obrigatório.");

        var promptBaseParam = await _parameterRepository.GetByNomeAsync("Prompt_Base", cancellationToken);
        if (promptBaseParam == null)
            return Result<ProjectAnalysis>.Failure("Prompt_Base não encontrado na tabela de parâmetros.");

        var prompt = BuildAnalysisPrompt(promptBaseParam.Valor, data, additionalDocumentContext);

        var requestBody = new
        {
            model = _model,
            max_tokens = 8192,
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
            Console.WriteLine("[DEBUG] Resposta bruta do Claude:");
            Console.WriteLine($"[DEBUG] Tamanho da resposta: {responseContent.Length} caracteres");
            Console.WriteLine(responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent);
            Console.WriteLine("[DEBUG] ==================");

            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = claudeResponse?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            Console.WriteLine("[DEBUG] Texto extraído do Claude:");
            Console.WriteLine($"[DEBUG] Tamanho do texto: {text.Length} caracteres");
            Console.WriteLine(text.Length > 1000 ? text.Substring(0, 1000) + "..." : text);
            Console.WriteLine("[DEBUG] ==================");
            var analysis = ParseAnalysis(text, prompt);

            var projects = await _projectRepository.FindByNameAsync(data.ProjectName, cancellationToken);
            var project = projects.FirstOrDefault();

            if (project != null)
            {
                project.UpdatePromptEnviado(prompt);
                await _projectRepository.UpdateAsync(project, cancellationToken);

                // Criar as tarefas no Board baseado nas sugestões da IA
                if (analysis.Tasks != null && analysis.Tasks.Count > 0)
                {
                    var boards = new List<Domain.Entities.Board>();

                    foreach (var task in analysis.Tasks)
                    {
                        // Buscar usuário sugerido pelo nome na lista de membros da equipe
                        Guid? sugestaoResponsavelId = null;
                        if (!string.IsNullOrWhiteSpace(task.SuggestedResponsible))
                        {
                            var teamMember = data.TeamMembers?.FirstOrDefault(m => 
                                m.UserName.Equals(task.SuggestedResponsible, StringComparison.OrdinalIgnoreCase));

                            if (teamMember != null && Guid.TryParse(teamMember.UserId, out var userId))
                            {
                                sugestaoResponsavelId = userId;
                                Console.WriteLine($"[DEBUG] Usuário sugerido '{task.SuggestedResponsible}' encontrado: {userId}");
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] Usuário sugerido '{task.SuggestedResponsible}' não encontrado nos membros da equipe para a tarefa '{task.Name}'");
                            }
                        }

                        var board = new Domain.Entities.Board(
                            projectId: project.Id,
                            name: task.Name,
                            description: task.Description,
                            status: "A Fazer",
                            priority: task.Priority,
                            sugestaoResponsavelId: sugestaoResponsavelId,
                            prazoEmDias: task.DeadlineInDays,
                            ordemNoBoard: task.Order
                        );

                        // Atribuir o criador do projeto como responsável inicial da tarefa
                        board.AssignResponsavel(project.UserId);
                        boards.Add(board);
                    }

                    await _boardRepository.AddRangeAsync(boards, cancellationToken);
                    Console.WriteLine($"[DEBUG] {boards.Count} tarefas criadas no Board para o projeto {project.Name} (Responsável: {project.UserId})");
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                Console.WriteLine($"[DEBUG] ATENÇÃO: Projeto '{data.ProjectName}' NÃO foi encontrado!");
            }

            return Result<ProjectAnalysis>.Success(analysis, $"Análise gerada com sucesso. {analysis.Tasks?.Count ?? 0} tarefas criadas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Erro ao processar resposta do Claude: {ex.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
            }
            return Result<ProjectAnalysis>.Failure($"Não foi possível processar a resposta do Claude: {ex.Message}");
        }
    }

    private static string BuildAnalysisPrompt(string template, ProjectAnalysisInput data, string? additionalDocumentContext = null)
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
        var whatWentWellText = string.IsNullOrWhiteSpace(data.WhatWentWell) ? "N/A" : data.WhatWentWell;
        var whatWentWrongText = string.IsNullOrWhiteSpace(data.WhatWentWrong) ? "N/A" : data.WhatWentWrong;
        var observationsText = string.IsNullOrWhiteSpace(data.FinalObservations) ? "Nenhuma" : data.FinalObservations;

        return template
            .Replace("{ProjectName}", data.ProjectName)
            .Replace("{Objective}", data.Objective)
            .Replace("{Description}", descriptionText)
            .Replace("{StartDate}", data.StartDate)
            .Replace("{EndDate}", endDateText)
            .Replace("{Company}", data.Company)
            .Replace("{Department}", data.Department)
            .Replace("{ProjectType}", data.ProjectType)
            .Replace("{TeamMembers}", membersText)
            .Replace("{Budget}", budgetText)
            .Replace("{WorkSchedule}", workText)
            .Replace("{DowntimePolicy}", downtimeText)
            .Replace("{ExternalDependencies}", depsText)
            .Replace("{Integrations}", intgText)
            .Replace("{Compliance}", complianceText)
            .Replace("{ComplianceApprovers}", approversText)
            .Replace("{UnavailablePeriods}", periodsText)
            .Replace("{PriorityRanking}", prioritiesText)
            .Replace("{BiggestRisk}", biggestRiskText)
            .Replace("{PreviousExperience}", experienceText)
            .Replace("{WhatWentWell}", whatWentWellText)
            .Replace("{WhatWentWrong}", whatWentWrongText)
            .Replace("{DetailLevel}", detailText)
            .Replace("{ReviewFrequency}", reviewText)
            .Replace("{FinalObservations}", observationsText)
            + BuildAdditionalDocumentContext(additionalDocumentContext);
    }

    private static string BuildAdditionalDocumentContext(string? additionalDocumentContext)
    {
        if (string.IsNullOrWhiteSpace(additionalDocumentContext))
            return string.Empty;

        return $"\n\n## CONTEXTO ADICIONAL DE DOCUMENTOS\n\nOs seguintes documentos foram fornecidos para fornecer contexto adicional sobre o projeto:\n\n{additionalDocumentContext}\n\nConsidere essas informações ao gerar a análise, tarefas e recomendações.";
    }

    private static ProjectAnalysis ParseAnalysis(string text, string promptSent)
    {
        text = text.Trim();

        Console.WriteLine("[DEBUG] ParseAnalysis - Texto original:");
        Console.WriteLine(text.Length > 500 ? text.Substring(0, 500) + "..." : text);

        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                text = text[start..(end + 1)];
                Console.WriteLine("[DEBUG] ParseAnalysis - Texto após remover markdown:");
                Console.WriteLine(text.Length > 500 ? text.Substring(0, 500) + "..." : text);
            }
        }

        // Validar se o JSON está completo
        if (!IsValidJsonStructure(text))
        {
            Console.WriteLine("[WARNING] JSON parece estar incompleto. Tentando corrigir...");
            text = TryFixIncompleteJson(text);
            Console.WriteLine("[DEBUG] JSON após correção:");
            Console.WriteLine(text.Length > 500 ? text.Substring(text.Length - 500) : text);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ClaudeAnalysisJson>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Console.WriteLine($"[DEBUG] ParseAnalysis - Overview: {parsed?.Overview?.Substring(0, Math.Min(50, parsed.Overview?.Length ?? 0))}...");
            Console.WriteLine($"[DEBUG] ParseAnalysis - Tasks Count: {parsed?.Tasks?.Count ?? 0}");

            var tasks = parsed?.Tasks?.Select(t => new TaskSuggestion(
                t.Name ?? "Tarefa sem nome",
                t.Description,
                t.Priority ?? "Média",
                t.SuggestedResponsible,
                t.DeadlineInDays,
                t.Order
            )).ToList() ?? new List<TaskSuggestion>();

            Console.WriteLine($"[DEBUG] ParseAnalysis - Tarefas processadas: {tasks.Count}");

            return new ProjectAnalysis(
                parsed?.Overview ?? string.Empty,
                parsed?.Risks ?? string.Empty,
                parsed?.Recommendations ?? string.Empty,
                tasks,
                promptSent
            );
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"[ERROR] Erro de desserialização JSON: {jsonEx.Message}");
            Console.WriteLine($"[ERROR] Path: {jsonEx.Path}");
            Console.WriteLine($"[ERROR] LineNumber: {jsonEx.LineNumber}");
            Console.WriteLine($"[ERROR] Últimos 1000 caracteres do JSON:");
            Console.WriteLine(text.Length > 1000 ? text.Substring(text.Length - 1000) : text);
            throw;
        }
    }

    private static bool IsValidJsonStructure(string json)
    {
        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        bool escaped = false;

        foreach (char c in json)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                else if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
            }
        }

        return braceCount == 0 && bracketCount == 0;
    }

    private static string TryFixIncompleteJson(string json)
    {
        // Contar abertura e fechamento de chaves e colchetes
        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        bool escaped = false;

        foreach (char c in json)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                else if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
            }
        }

        // Remover último objeto/elemento incompleto se necessário
        var result = new StringBuilder(json);

        // Se estamos dentro de uma string, fechar a string
        if (inString)
        {
            result.Append('"');
        }

        // Procurar última vírgula antes de fechar arrays/objetos incompletos
        if (bracketCount > 0 || braceCount > 0)
        {
            // Remover conteúdo incompleto após a última vírgula válida
            int lastCommaIndex = -1;
            int currentBraces = braceCount;
            int currentBrackets = bracketCount;

            for (int i = result.Length - 1; i >= 0; i--)
            {
                char c = result[i];
                if (c == '}') currentBraces++;
                else if (c == '{') currentBraces--;
                else if (c == ']') currentBrackets++;
                else if (c == '[') currentBrackets--;
                else if (c == ',' && currentBraces == braceCount && currentBrackets == bracketCount)
                {
                    lastCommaIndex = i;
                    break;
                }
            }

            if (lastCommaIndex > 0)
            {
                result.Length = lastCommaIndex;
            }
        }

        // Fechar arrays abertos
        for (int i = 0; i < bracketCount; i++)
        {
            result.Append(']');
        }

        // Fechar objetos abertos
        for (int i = 0; i < braceCount; i++)
        {
            result.Append('}');
        }

        return result.ToString();
    }
}
