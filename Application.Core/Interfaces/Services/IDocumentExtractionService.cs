using Domain.Common;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface para serviço de extração de texto de documentos
/// </summary>
public interface IDocumentExtractionService
{
    /// <summary>
    /// Extrai texto de um arquivo enviado
    /// </summary>
    /// <param name="fileStream">Stream do arquivo</param>
    /// <param name="fileName">Nome do arquivo com extensão</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado contendo o texto extraído</returns>
    Task<Result<string>> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se o arquivo é suportado
    /// </summary>
    /// <param name="fileName">Nome do arquivo com extensão</param>
    /// <param name="fileSize">Tamanho do arquivo em bytes</param>
    /// <returns>Resultado da validação</returns>
    Result ValidateFile(string fileName, long fileSize);
}
