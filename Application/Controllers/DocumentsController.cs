using Application.Core.DTOs.Documents.Requests;
using Application.Core.DTOs.Documents.Responses;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Controller para extração de texto de documentos
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentExtractionService _documentExtractionService;

    public DocumentsController(IDocumentExtractionService documentExtractionService)
    {
        _documentExtractionService = documentExtractionService;
    }

    /// <summary>
    /// Extrai texto de um arquivo enviado (PDF, Word, Excel, TXT)
    /// </summary>
    /// <param name="request">Request contendo o arquivo</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Texto extraído do documento</returns>
    [HttpPost("extrair-texto")]
    [ProducesResponseType(typeof(Result<ExtractedTextResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ExtractText(
        [FromForm] ExtractTextRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(Result.Failure("Nenhum arquivo foi enviado."));
        }

        // Validar arquivo antes de processar
        var validation = _documentExtractionService.ValidateFile(
            request.File.FileName, 
            request.File.Length);

        if (!validation.IsSuccess)
        {
            return BadRequest(Result.Failure(validation.Message));
        }

        // Extrair texto
        await using var stream = request.File.OpenReadStream();
        var result = await _documentExtractionService.ExtractTextAsync(
            stream, 
            request.File.FileName, 
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(Result<ExtractedTextResponse>.Failure(result.Message));
        }

        var response = new ExtractedTextResponse
        {
            ExtractedText = result.Data!,
            FileName = request.File.FileName,
            FileSize = request.File.Length,
            FileExtension = Path.GetExtension(request.File.FileName),
            CharacterCount = result.Data!.Length
        };

        return Ok(Result<ExtractedTextResponse>.Success(
            response, 
            $"Texto extraído com sucesso de '{request.File.FileName}'."));
    }
}
