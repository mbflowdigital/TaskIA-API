using Application.Core.Interfaces.Services;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

public record SuggestProjectRequest(string ProjectName);
public record ProjectSuggestionResponse(string Description, string Objective);
public record ProjectAnalysisResponse(string Overview, string Risks, string Recommendations, string PromptSent);
public record AnalyzeProjectJobDto(string JobId);
public record AnalyzeProjectStatusDto(string Status, ProjectAnalysisResponse? Result, string? ErrorMessage);
public record GenerateTasksRequest(Guid ProjectId);
public record GenerateTasksJobDto(string JobId);
public record GenerateTasksStatusDto(string Status, int TasksCreated, string? ErrorMessage);
public record TaskDto(string Name, string? Description, string Priority, string? SuggestedResponsible, int DeadlineInDays, decimal Order);
public record GenerateTasksResponse(List<TaskDto> Tasks, int TasksCreated, string PromptSent);

/// <summary>
/// Controller de integração com Claude AI
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaudeController : ControllerBase
{
    private readonly ClaudeService _claudeService;
    private readonly IDocumentExtractionService _documentExtractionService;
    private readonly TaskJobStore _jobStore;
    private readonly IServiceScopeFactory _scopeFactory;

    public ClaudeController(
        ClaudeService claudeService,
        IDocumentExtractionService documentExtractionService,
        TaskJobStore jobStore,
        IServiceScopeFactory scopeFactory)
    {
        _claudeService = claudeService;
        _documentExtractionService = documentExtractionService;
        _jobStore = jobStore;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Gera descrição e objetivo para um projeto com base no nome informado
    /// </summary>
    [HttpPost("suggest-project")]
    [ProducesResponseType(typeof(Result<ProjectSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuggestProject(
        [FromBody] SuggestProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.ProjectName))
            return BadRequest(Result.Failure("O nome do projeto é obrigatório."));

        var result = await _claudeService.SuggestProjectAsync(request.ProjectName, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(Result.Failure(result.Message));

        var response = Result<ProjectSuggestionResponse>.Success(
            new ProjectSuggestionResponse(result.Data!.Description, result.Data.Objective),
            result.Message
        );

        return Ok(response);
    }

    /// <summary>
    /// Inicia a análise do projeto em background e retorna um jobId para polling.
    /// </summary>
    [HttpPost("analyze-project")]
    [ProducesResponseType(typeof(Result<AnalyzeProjectJobDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public IActionResult AnalyzeProject([FromBody] ProjectAnalysisInput request)
    {
        if (string.IsNullOrWhiteSpace(request?.ProjectName))
            return BadRequest(Result.Failure("O nome do projeto é obrigatório."));

        var jobId = _jobStore.CreateJob();

        _ = Task.Run(async () =>
        {
            _jobStore.SetRunning(jobId);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var claudeService = scope.ServiceProvider.GetRequiredService<ClaudeService>();

                var result = await claudeService.AnalyzeProjectAsync(request, null);

                if (result.IsSuccess && result.Data != null)
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(
                        new ProjectAnalysisResponse(
                            result.Data.Overview,
                            result.Data.Risks,
                            result.Data.Recommendations,
                            result.Data.PromptSent));
                    _jobStore.SetCompletedWithPayload(jobId, payload);
                }
                else
                {
                    _jobStore.SetFailed(jobId, result.Message);
                }
            }
            catch (Exception ex)
            {
                _jobStore.SetFailed(jobId, $"Erro inesperado: {ex.Message}");
            }
        });

        return Accepted(Result<AnalyzeProjectJobDto>.Success(new AnalyzeProjectJobDto(jobId), "Job iniciado."));
    }

    /// <summary>
    /// Retorna o status de um job de análise de projeto.
    /// </summary>
    [HttpGet("analyze-project/{jobId}/status")]
    [ProducesResponseType(typeof(Result<AnalyzeProjectStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public IActionResult GetAnalyzeProjectStatus(string jobId)
    {
        var job = _jobStore.Get(jobId);
        if (job == null)
            return NotFound(Result.Failure("Job não encontrado."));

        ProjectAnalysisResponse? analysisResult = null;
        if (job.Status == TaskJobStatus.Completed && job.ResultPayload != null)
        {
            analysisResult = System.Text.Json.JsonSerializer.Deserialize<ProjectAnalysisResponse>(
                job.ResultPayload,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        var dto = new AnalyzeProjectStatusDto(job.Status.ToString(), analysisResult, job.ErrorMessage);
        return Ok(Result<AnalyzeProjectStatusDto>.Success(dto));
    }

    /// <summary>
    /// Inicia a geração de tarefas em background e retorna um jobId para polling.
    /// </summary>
    [HttpPost("generate-tasks")]
    [ProducesResponseType(typeof(Result<GenerateTasksJobDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public IActionResult GenerateTasks([FromBody] GenerateTasksRequest request)
    {
        if (request == null || request.ProjectId == Guid.Empty)
            return BadRequest(Result.Failure("O ID do projeto é obrigatório."));

        var jobId = _jobStore.CreateJob();

        // Executa em background sem bloquear a resposta HTTP
        _ = Task.Run(async () =>
        {
            _jobStore.SetRunning(jobId);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var claudeService = scope.ServiceProvider.GetRequiredService<ClaudeService>();

                var input = new GenerateTasksInput(request.ProjectId);
                var result = await claudeService.GenerateProjectTasksAsync(input);

                if (result.IsSuccess)
                    _jobStore.SetCompleted(jobId, result.Data!.TasksCreated);
                else
                    _jobStore.SetFailed(jobId, result.Message);
            }
            catch (Exception ex)
            {
                _jobStore.SetFailed(jobId, $"Erro inesperado: {ex.Message}");
            }
        });

        return Accepted(Result<GenerateTasksJobDto>.Success(new GenerateTasksJobDto(jobId), "Job iniciado."));
    }

    /// <summary>
    /// Retorna o status de um job de geração de tarefas.
    /// </summary>
    [HttpGet("generate-tasks/{jobId}/status")]
    [ProducesResponseType(typeof(Result<GenerateTasksStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public IActionResult GetGenerateTasksStatus(string jobId)
    {
        var job = _jobStore.Get(jobId);
        if (job == null)
            return NotFound(Result.Failure("Job não encontrado."));

        var dto = new GenerateTasksStatusDto(
            job.Status.ToString(),
            job.TasksCreated,
            job.ErrorMessage
        );

        return Ok(Result<GenerateTasksStatusDto>.Success(dto));
    }

}
