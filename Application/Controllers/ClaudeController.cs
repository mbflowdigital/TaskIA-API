using Application.Core.Interfaces.Services;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

public record SuggestProjectRequest(string ProjectName);
public record ProjectSuggestionResponse(string Description, string Objective);
public record ProjectAnalysisResponse(string Overview, string Risks, string Recommendations, string PromptSent);
public record GenerateTasksRequest(Guid ProjectId);
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

    public ClaudeController(
        ClaudeService claudeService,
        IDocumentExtractionService documentExtractionService)
    {
        _claudeService = claudeService;
        _documentExtractionService = documentExtractionService;
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
    /// Analisa um projeto com base em todos os dados preenchidos e gera insights via Claude Sonnet
    /// </summary>
    [HttpPost("analyze-project")]
    [ProducesResponseType(typeof(Result<ProjectAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeProject(
        [FromBody] ProjectAnalysisInput request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.ProjectName))
            return BadRequest(Result.Failure("O nome do projeto é obrigatório."));

        var result = await _claudeService.AnalyzeProjectAsync(request, null, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(Result.Failure(result.Message));

        var response = Result<ProjectAnalysisResponse>.Success(
            new ProjectAnalysisResponse(result.Data!.Overview, result.Data.Risks, result.Data.Recommendations, result.Data.PromptSent),
            result.Message
        );

        return Ok(response);
    }

    /// <summary>
    /// Gera tarefas para um projeto baseado na análise previamente aprovada
    /// </summary>
    [HttpPost("generate-tasks")]
    [ProducesResponseType(typeof(Result<GenerateTasksResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateTasks(
        [FromBody] GenerateTasksRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.ProjectId == Guid.Empty)
            return BadRequest(Result.Failure("O ID do projeto é obrigatório."));

        var input = new GenerateTasksInput(request.ProjectId);
        var result = await _claudeService.GenerateProjectTasksAsync(input, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(Result.Failure(result.Message));

        var tasks = result.Data!.Tasks.Select(t => new TaskDto(
            t.Name,
            t.Description,
            t.Priority,
            t.SuggestedResponsible,
            t.DeadlineInDays,
            t.Order
        )).ToList();

        var response = Result<GenerateTasksResponse>.Success(
            new GenerateTasksResponse(tasks, result.Data.TasksCreated, result.Data.PromptSent),
            result.Message
        );

        return Ok(response);
    }

}
