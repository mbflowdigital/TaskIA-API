using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

public record SuggestProjectRequest(string ProjectName);
public record ProjectSuggestionResponse(string Description, string Objective);

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

    public ClaudeController(ClaudeService claudeService)
    {
        _claudeService = claudeService;
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
}
