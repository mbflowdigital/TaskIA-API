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

    /// <summary>
    /// Analisa um projeto com contexto adicional de documentos (PDF, Word, Excel, TXT)
    /// </summary>
    [HttpPost("analyze-project-with-documents")]
    [ProducesResponseType(typeof(Result<ProjectAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> AnalyzeProjectWithDocuments(
        [FromForm] string projectDataJson,
        [FromForm] List<IFormFile>? documents,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectDataJson))
            return BadRequest(Result.Failure("Os dados do projeto são obrigatórios."));

        ProjectAnalysisInput? request;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<ProjectAnalysisInput>(
                projectDataJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
                return BadRequest(Result.Failure("Não foi possível deserializar os dados do projeto."));
        }
        catch (Exception ex)
        {
            return BadRequest(Result.Failure($"Erro ao processar dados do projeto: {ex.Message}"));
        }

        if (string.IsNullOrWhiteSpace(request.ProjectName))
            return BadRequest(Result.Failure("O nome do projeto é obrigatório."));

        // Extrair texto dos documentos, se fornecidos
        string? additionalContext = null;
        if (documents != null && documents.Count > 0)
        {
            var extractedTexts = new List<string>();

            foreach (var document in documents)
            {
                if (document.Length == 0)
                    continue;

                await using var stream = document.OpenReadStream();
                var extractResult = await _documentExtractionService.ExtractTextAsync(
                    stream,
                    document.FileName,
                    cancellationToken);

                if (extractResult.IsSuccess)
                {
                    extractedTexts.Add($"### Documento: {document.FileName}\n\n{extractResult.Data}");
                }
                else
                {
                    Console.WriteLine($"[WARNING] Falha ao extrair texto de '{document.FileName}': {extractResult.Message}");
                }
            }

            if (extractedTexts.Count > 0)
            {
                additionalContext = string.Join("\n\n---\n\n", extractedTexts);
                Console.WriteLine($"[DEBUG] Contexto adicional extraído de {extractedTexts.Count} documento(s). Total: {additionalContext.Length} caracteres");
            }
        }

        // Analisar projeto com contexto adicional
        var result = await _claudeService.AnalyzeProjectAsync(request, additionalContext, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(Result.Failure(result.Message));

        var response = Result<ProjectAnalysisResponse>.Success(
            new ProjectAnalysisResponse(result.Data!.Overview, result.Data.Risks, result.Data.Recommendations, result.Data.PromptSent),
            result.Message
        );

        return Ok(response);
    }
}
