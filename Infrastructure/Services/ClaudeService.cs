using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Common;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public record ProjectSuggestion(string Description, string Objective);
public record TaskSuggestion(string Name, string? Description, string Priority, string? SuggestedResponsible, int DeadlineInDays, decimal Order, decimal? ParentTaskOrder);
public record ProjectAnalysis(string Overview, string Risks, string Recommendations, List<TaskSuggestion>? Tasks, string PromptSent);
public record GenerateTasksInput(Guid ProjectId);
public record TasksGenerationResult(List<TaskSuggestion> Tasks, int TasksCreated, string PromptSent);
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

        [JsonPropertyName("parentTaskOrder")]
        public decimal? ParentTaskOrder { get; set; }
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

        // Calcular e logar tamanho do prompt
        var estimatedPromptTokens = prompt.Length / 4;
        Console.WriteLine($"[INFO] ========================================");
        Console.WriteLine($"[INFO] Preparando análise completa com IA...");
        Console.WriteLine($"[INFO] Tamanho do prompt: {prompt.Length} caracteres (~{estimatedPromptTokens} tokens)");
        Console.WriteLine($"[INFO] Max tokens de resposta: 8192");
        Console.WriteLine($"[INFO] Timeout configurado: 10 minutos");
        Console.WriteLine($"[INFO] ========================================");

        var requestBody = new
        {
            model = _model,
            max_tokens = 8192, // Mantido em 8192 para análise completa
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
            Console.WriteLine($"[INFO] Enviando requisição para Claude API: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine($"[INFO] Modelo: {_model}");
            Console.WriteLine($"[INFO] Aguarde... Isso pode levar alguns minutos para análises complexas.");

            var startTime = DateTime.Now;
            response = await _httpClient.SendAsync(request, cancellationToken);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;

            Console.WriteLine($"[SUCCESS] ✓ Resposta recebida em {elapsed:F2} segundos ({elapsed / 60:F1} minutos)");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ERROR] ✗ Timeout: A análise ultrapassou o limite de 10 minutos");
            Console.WriteLine($"[ERROR] Detalhes: {ex.Message}");
            return Result<ProjectAnalysis>.Failure(
                "A análise está demorando mais que o esperado (>10 minutos). " +
                "Isso pode acontecer com projetos muito complexos ou com muitos documentos anexados. " +
                "Sugestões: " +
                "1) Reduza a quantidade de membros da equipe ou dependências; " +
                "2) Escolha nível de detalhe 'Macro' ao invés de 'Granular'; " +
                "3) Remova documentos anexados muito grandes; " +
                "4) Tente novamente em alguns minutos.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] ✗ Erro de conexão com Claude API: {ex.Message}");
            return Result<ProjectAnalysis>.Failure($"Erro de conexão com a API do Claude: {ex.Message}. Verifique sua conexão com a internet.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ✗ Erro inesperado: {ex.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            return Result<ProjectAnalysis>.Failure($"Erro inesperado ao comunicar com Claude: {ex.Message}");
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
                // Atualizar prompt enviado
                project.UpdatePromptEnviado(prompt);

                // Salvar resultados da análise da IA
                project.UpdateAnalysisResults(
                    analysis.Overview,
                    analysis.Risks,
                    analysis.Recommendations
                );

                // Mudar status para "Pendente Aprovação"
                project.UpdateStatus("Waiting_Approve");

                await _projectRepository.UpdateAsync(project, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"[SUCCESS] ✓ Análise salva e projeto '{project.Name}' marcado como 'Pendente Aprovação'");
            }
            else
            {
                Console.WriteLine($"[DEBUG] ATENÇÃO: Projeto '{data.ProjectName}' NÃO foi encontrado!");
            }

            return Result<ProjectAnalysis>.Success(analysis, "Análise gerada com sucesso. Aguardando aprovação do usuário para gerar tarefas.");
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

        // Log do tamanho do contexto adicional
        Console.WriteLine($"[INFO] Contexto adicional de documentos: {additionalDocumentContext.Length} caracteres (~{additionalDocumentContext.Length / 4} tokens)");

        // Enviar contexto COMPLETO - sem limitações
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
                t.Order,
                t.ParentTaskOrder
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

    /// <summary>
    /// Gera tarefas para um projeto baseado na análise previamente salva
    /// </summary>
    public async Task<Result<TasksGenerationResult>> GenerateProjectTasksAsync(
        GenerateTasksInput input,
        CancellationToken cancellationToken = default)
    {
        // 1. Buscar projeto
        var project = await _projectRepository.GetByIdAsync(input.ProjectId, cancellationToken);
        if (project == null)
            return Result<TasksGenerationResult>.Failure("Projeto não encontrado.");

        // 2. Validar status
        if (project.Status != "Waiting_Approve")
            return Result<TasksGenerationResult>.Failure($"Projeto deve estar com status 'Waiting_Approve'. Status atual: '{project.Status}'");

        // 3. Validar se análise existe
        if (string.IsNullOrWhiteSpace(project.IA_Overview) || 
            string.IsNullOrWhiteSpace(project.IA_Risks) || 
            string.IsNullOrWhiteSpace(project.IA_Recommendations))
            return Result<TasksGenerationResult>.Failure("Análise da IA não encontrada. Execute a análise primeiro.");

        // 4. Buscar dados do projeto com relacionamentos
        var projectDetails = await _context.Projects
            .Include(p => p.ProjectMembers)
                .ThenInclude(pm => pm.User)
            .Include(p => p.ProjectDetails)
            .Include(p => p.ExecutionSettings)
            .FirstOrDefaultAsync(p => p.Id == input.ProjectId, cancellationToken);

        if (projectDetails == null)
            return Result<TasksGenerationResult>.Failure("Detalhes do projeto não encontrados.");

        // 5. Buscar prompt template
        var promptTaskParam = await _parameterRepository.GetByNomeAsync("Prompt_Task", cancellationToken);
        if (promptTaskParam == null)
            return Result<TasksGenerationResult>.Failure("Prompt_Task não encontrado na tabela de parâmetros.");

        // 6. Montar prompt substituindo placeholders
        var prompt = BuildTasksPrompt(promptTaskParam.Valor, projectDetails);

        Console.WriteLine($"[INFO] ========================================");
        Console.WriteLine($"[INFO] Gerando tarefas para projeto '{project.Name}'...");
        Console.WriteLine($"[INFO] Tamanho do prompt: {prompt.Length} caracteres");
        Console.WriteLine($"[INFO] ========================================");

        // 7. Chamar Claude API
        var requestBody = new
        {
            model = _model,
            max_tokens = 16000,
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
            Console.WriteLine($"[INFO] Enviando requisição para Claude API: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            var startTime = DateTime.Now;
            response = await _httpClient.SendAsync(request, cancellationToken);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"[SUCCESS] ✓ Resposta recebida em {elapsed:F2} segundos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ✗ Erro ao comunicar com Claude: {ex.Message}");
            return Result<TasksGenerationResult>.Failure($"Erro ao comunicar com Claude: {ex.Message}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<TasksGenerationResult>.Failure($"Claude API retornou erro {(int)response.StatusCode}.");

        try
        {
            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = claudeResponse?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            // 8. Parsear resposta (array de tarefas)
            var tasks = ParseTasksArray(text);

            if (tasks.Count == 0)
                return Result<TasksGenerationResult>.Failure("Nenhuma tarefa foi gerada pela IA.");

            // 9. Separar tarefas principais de subtarefas
            var mainTasks = tasks.Where(t => t.ParentTaskOrder == null).ToList();
            var subTasks = tasks.Where(t => t.ParentTaskOrder != null).ToList();

            Console.WriteLine($"[INFO] Total de tarefas: {tasks.Count} (Principais: {mainTasks.Count}, Subtarefas: {subTasks.Count})");

            var teamMembers = projectDetails.ProjectMembers.ToList();

            // Dicionário para mapear IDs temporários (da IA) para IDs reais (do banco)
            var taskIdMapping = new Dictionary<Guid, Guid>();

            // 10. Criar tarefas principais primeiro
            var mainBoards = new List<Domain.Entities.Board>();

            foreach (var task in mainTasks)
            {
                // Buscar usuário sugerido
                Guid? sugestaoResponsavelId = null;
                if (!string.IsNullOrWhiteSpace(task.SuggestedResponsible))
                {
                    var teamMember = teamMembers.FirstOrDefault(m => 
                        m.User != null && m.User.Name.Equals(task.SuggestedResponsible, StringComparison.OrdinalIgnoreCase));

                    if (teamMember != null)
                    {
                        sugestaoResponsavelId = teamMember.UserId;
                        Console.WriteLine($"[DEBUG] Usuário sugerido '{task.SuggestedResponsible}' encontrado: {teamMember.UserId}");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Usuário sugerido '{task.SuggestedResponsible}' não encontrado para tarefa '{task.Name}'");
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
                    ordemNoBoard: task.Order,
                    parentTaskId: null // Tarefas principais não têm pai
                );

                // Atribuir criador do projeto como responsável inicial
                board.AssignResponsavel(project.UserId);
                mainBoards.Add(board);

                // Mapear ID temporário (gerado pela IA) para ID real (do banco)
                // IMPORTANTE: Precisamos de um identificador único da tarefa da IA
                // Como não temos, vamos usar a Order como chave temporária
                Console.WriteLine($"[DEBUG] Tarefa principal '{task.Name}' criada com ID: {board.Id} (Order: {task.Order})");
            }

            // Salvar tarefas principais
            await _boardRepository.AddRangeAsync(mainBoards, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[SUCCESS] ✓ {mainBoards.Count} tarefas principais criadas no banco");

            // 11. Criar dicionário de mapeamento usando Order como chave
            // (assumindo que Order é único por tarefa conforme instruções do prompt)
            var orderToIdMap = mainBoards.ToDictionary(b => b.OrdemNoBoard, b => b.Id);

            // 12. Criar subtarefas usando IDs reais das tarefas pai
            var subBoards = new List<Domain.Entities.Board>();

            foreach (var task in subTasks)
            {
                // Mapear ParentTaskOrder (decimal) para ParentTaskId (Guid) real
                Guid? parentTaskId = null;
                if (task.ParentTaskOrder.HasValue)
                {
                    if (orderToIdMap.TryGetValue(task.ParentTaskOrder.Value, out var parentId))
                    {
                        parentTaskId = parentId;
                        Console.WriteLine($"[DEBUG] Subtarefa '{task.Name}' linkada à tarefa pai com Order {task.ParentTaskOrder.Value} (ID: {parentId})");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Tarefa pai com Order {task.ParentTaskOrder.Value} não encontrada para subtarefa '{task.Name}'. Criando como tarefa principal.");
                    }
                }

                // Buscar usuário sugerido
                Guid? sugestaoResponsavelId = null;
                if (!string.IsNullOrWhiteSpace(task.SuggestedResponsible))
                {
                    var teamMember = teamMembers.FirstOrDefault(m => 
                        m.User != null && m.User.Name.Equals(task.SuggestedResponsible, StringComparison.OrdinalIgnoreCase));

                    if (teamMember != null)
                    {
                        sugestaoResponsavelId = teamMember.UserId;
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
                    ordemNoBoard: task.Order,
                    parentTaskId: parentTaskId // ✓ Usando Guid mapeado do ParentTaskOrder
                );

                board.AssignResponsavel(project.UserId);
                subBoards.Add(board);
            }

            if (subBoards.Count > 0)
            {
                await _boardRepository.AddRangeAsync(subBoards, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                Console.WriteLine($"[SUCCESS] ✓ {subBoards.Count} subtarefas criadas no banco");
            }

            var totalCreated = mainBoards.Count + subBoards.Count;

            // 13. Mudar status do projeto para Active
            project.UpdateStatus("Active");
            await _projectRepository.UpdateAsync(project, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[SUCCESS] ✓ {totalCreated} tarefas criadas e projeto ativado!");

            var result = new TasksGenerationResult(tasks, totalCreated, prompt);
            return Result<TasksGenerationResult>.Success(result, $"{totalCreated} tarefas criadas com sucesso. Projeto ativado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Erro ao processar tarefas: {ex.Message}");
            return Result<TasksGenerationResult>.Failure($"Erro ao processar tarefas: {ex.Message}");
        }
    }

    private static string BuildTasksPrompt(string template, Domain.Entities.Project project)
    {
        var startDate = project.StartDate?.ToString("dd/MM/yyyy") ?? "Não definida";
        var endDate = project.EndDate?.ToString("dd/MM/yyyy") ?? "Não definida";

        var totalDays = 0;
        if (project.StartDate.HasValue && project.EndDate.HasValue)
        {
            totalDays = (project.EndDate.Value - project.StartDate.Value).Days;
        }

        var teamMembersSummary = project.ProjectMembers.Any()
            ? string.Join("\n", project.ProjectMembers
                .Where(m => m.User != null)
                .Select(m => $"{m.User!.Name} - {m.ProjectFunction ?? "Sem papel definido"} - {m.Dedication ?? "Dedicação não informada"}"))
            : "Nenhum membro definido";

        var detailLevel = project.ExecutionSettings?.NivelDetalhePlano.ToString() ?? "Balanceado";

        // Separar riscos por criticidade (simplificado - o prompt já vem formatado)
        var risks = project.IA_Risks ?? "Nenhum risco identificado";

        // Parse simples de riscos (assumindo formato com emojis)
        var criticalRisks = ExtractRisksByLevel(risks, "🔴", "🟠");
        var highRisks = ExtractRisksByLevel(risks, "🟠", "🟡");
        var mediumRisks = ExtractRisksByLevel(risks, "🟡", "🟢");
        var lowRisks = ExtractRisksByLevel(risks, "🟢", null);

        return template
            .Replace("{ProjectName}", project.Name)
            .Replace("{StartDate}", startDate)
            .Replace("{EndDate}", endDate)
            .Replace("{TotalDays}", totalDays.ToString())
            .Replace("{DetailLevel}", detailLevel)
            .Replace("{TeamMembersSummary}", teamMembersSummary)
            .Replace("{Overview}", project.IA_Overview ?? "")
            .Replace("{CriticalRisks}", criticalRisks)
            .Replace("{HighRisks}", highRisks)
            .Replace("{MediumRisks}", mediumRisks)
            .Replace("{LowRisks}", lowRisks)
            .Replace("{Recommendations}", project.IA_Recommendations ?? "");
    }

    private static string ExtractRisksByLevel(string allRisks, string startEmoji, string? endEmoji)
    {
        var startIndex = allRisks.IndexOf(startEmoji);
        if (startIndex == -1) return "Nenhum";

        var endIndex = endEmoji != null ? allRisks.IndexOf(endEmoji, startIndex + 1) : allRisks.Length;
        if (endIndex == -1) endIndex = allRisks.Length;

        var section = allRisks.Substring(startIndex, endIndex - startIndex).Trim();
        return string.IsNullOrWhiteSpace(section) ? "Nenhum" : section;
    }

    private static List<TaskSuggestion> ParseTasksArray(string text)
    {
        text = text.Trim();

        Console.WriteLine("[DEBUG] ParseTasksArray - Tamanho do texto: " + text.Length);
        Console.WriteLine("[DEBUG] ParseTasksArray - Últimos 200 chars: " + (text.Length > 200 ? text[^200..] : text));

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Extrair o bloco entre o primeiro '[' e o último ']'
        var arrayStart = text.IndexOf('[');
        var arrayEnd   = text.LastIndexOf(']');

        if (arrayStart < 0)
        {
            Console.WriteLine("[ERROR] Nenhum array JSON encontrado no texto.");
            return new List<TaskSuggestion>();
        }

        // Se não há ']' de fechamento o JSON foi truncado — tentar recuperar os objetos completos
        string candidate;
        if (arrayEnd <= arrayStart)
        {
            Console.WriteLine("[WARNING] JSON array truncado (sem ']' final) — tentando recuperação parcial.");
            candidate = RecoverTruncatedArray(text[arrayStart..]);
        }
        else
        {
            candidate = text[arrayStart..(arrayEnd + 1)];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<ClaudeTaskJson>>(candidate, options);
            if (parsed != null && parsed.Count > 0)
            {
                Console.WriteLine($"[SUCCESS] ✓ {parsed.Count} tarefas parseadas.");
                return parsed.Select(t => new TaskSuggestion(
                    t.Name ?? "Tarefa sem nome",
                    t.Description,
                    t.Priority ?? "Média",
                    t.SuggestedResponsible,
                    t.DeadlineInDays,
                    t.Order,
                    t.ParentTaskOrder
                )).ToList();
            }

            Console.WriteLine("[WARNING] Array de tarefas vazio ou nulo após parse.");
            return new List<TaskSuggestion>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] Erro ao parsear array de tarefas: {ex.Message}");
            Console.WriteLine("[INFO] Tentando recuperação parcial do JSON truncado...");

            // Última tentativa: recuperar objetos completos do array truncado
            var recovered = RecoverTruncatedArray(candidate);
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ClaudeTaskJson>>(recovered, options);
                if (parsed != null && parsed.Count > 0)
                {
                    Console.WriteLine($"[SUCCESS] ✓ {parsed.Count} tarefas recuperadas do JSON truncado.");
                    return parsed.Select(t => new TaskSuggestion(
                        t.Name ?? "Tarefa sem nome",
                        t.Description,
                        t.Priority ?? "Média",
                        t.SuggestedResponsible,
                        t.DeadlineInDays,
                        t.Order,
                        t.ParentTaskOrder
                    )).ToList();
                }
            }
            catch (JsonException ex2)
            {
                Console.WriteLine($"[ERROR] Recuperação parcial também falhou: {ex2.Message}");
            }

            return new List<TaskSuggestion>();
        }
    }

    /// <summary>
    /// Tenta recuperar um array JSON truncado mantendo apenas os objetos completos.
    /// Um objeto é considerado completo se tem chaves balanceadas e termina com '}'.
    /// </summary>
    private static string RecoverTruncatedArray(string truncated)
    {
        var items = new List<string>();
        var depth = 0;
        var start = -1;

        for (var i = 0; i < truncated.Length; i++)
        {
            if (truncated[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (truncated[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    items.Add(truncated[start..(i + 1)]);
                    start = -1;
                }
            }
        }

        return items.Count > 0 ? "[" + string.Join(",", items) + "]" : "[]";
    }
}
