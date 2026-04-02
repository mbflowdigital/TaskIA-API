using Application.Core.Interfaces.Services;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

public record SuggestProjectRequest(string ProjectName);
public record ProjectSuggestionResponse(string Description, string Objective);
public record ProjectAnalysisResponse(string Overview, string Risks, string Recommendations, string PromptSent);

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

}
