namespace Application.Core.DTOs.Documents.Responses;

/// <summary>
/// Response contendo o texto extraído do documento
/// </summary>
public record ExtractedTextResponse
{
    /// <summary>
    /// Texto extraído do documento
    /// </summary>
    public string ExtractedText { get; init; } = string.Empty;

    /// <summary>
    /// Nome do arquivo original
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Extensão do arquivo
    /// </summary>
    public string FileExtension { get; init; } = string.Empty;

    /// <summary>
    /// Quantidade de caracteres extraídos
    /// </summary>
    public int CharacterCount { get; init; }
}
