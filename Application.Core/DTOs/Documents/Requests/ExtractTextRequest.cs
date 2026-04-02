using Microsoft.AspNetCore.Http;

namespace Application.Core.DTOs.Documents.Requests;

/// <summary>
/// Request para extração de texto de documento
/// </summary>
public record ExtractTextRequest
{
    /// <summary>
    /// Arquivo a ser processado
    /// </summary>
    public IFormFile? File { get; init; }
}
