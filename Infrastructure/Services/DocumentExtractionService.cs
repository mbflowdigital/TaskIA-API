using Application.Core.Interfaces.Services;
using Domain.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using ClosedXML.Excel;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Serviço para extração de texto de documentos
/// </summary>
public class DocumentExtractionService : IDocumentExtractionService
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20    MB

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".xls",
        ".xlsx",
        ".txt"
    };

    /// <summary>
    /// Valida se o arquivo é suportado e não excede o tamanho máximo
    /// </summary>
    public Result ValidateFile(string fileName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure("Nome do arquivo é obrigatório.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
            return Result.Failure("Arquivo deve ter uma extensão válida.");

        if (!SupportedExtensions.Contains(extension))
        {
            return Result.Failure(
                $"Formato de arquivo não suportado. Formatos aceitos: {string.Join(", ", SupportedExtensions)}");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            return Result.Failure(
                $"Arquivo excede o tamanho máximo permitido de {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        if (fileSize == 0)
            return Result.Failure("Arquivo está vazio.");

        return Result.Success("Arquivo válido.");
    }

    /// <summary>
    /// Extrai texto do arquivo
    /// </summary>
    public async Task<Result<string>> ExtractTextAsync(
        Stream fileStream, 
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || !fileStream.CanRead)
            return Result<string>.Failure("Stream do arquivo é inválido.");

        var validation = ValidateFile(fileName, fileStream.Length);
        if (!validation.IsSuccess)
            return Result<string>.Failure(validation.Message);

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            string extractedText;

            switch (extension)
            {
                case ".pdf":
                    extractedText = await ExtractFromPdfAsync(fileStream, cancellationToken);
                    break;
                case ".docx":
                    extractedText = await ExtractFromDocxAsync(fileStream, cancellationToken);
                    break;
                case ".xls":
                case ".xlsx":
                    extractedText = await ExtractFromExcelAsync(fileStream, cancellationToken);
                    break;
                case ".txt":
                    extractedText = await ExtractFromTxtAsync(fileStream, cancellationToken);
                    break;
                case ".doc":
                    return Result<string>.Failure(
                        $"Formato {extension} não é suportado. Por favor, converta para o formato .docx.");
                default:
                    return Result<string>.Failure($"Formato de arquivo '{extension}' não suportado.");
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Result<string>.Failure(
                    "Não foi possível extrair texto do arquivo. O arquivo pode estar vazio ou corrompido.");
            }

            var cleanedText = CleanExtractedText(extractedText);

            return Result<string>.Success(
                cleanedText, 
                $"Texto extraído com sucesso. Total de caracteres: {cleanedText.Length}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(
                $"Erro ao extrair texto do arquivo: {ex.Message}");
        }
    }

    private async Task<string> ExtractFromPdfAsync(Stream stream, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; 

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var text = new StringBuilder();
        using var pdfReader = new PdfReader(memoryStream);
        using var pdfDocument = new PdfDocument(pdfReader);

        for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
        {
            var strategy = new SimpleTextExtractionStrategy();
            var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
            text.AppendLine(pageText);
        }

        return text.ToString();
    }

    private async Task<string> ExtractFromDocxAsync(Stream stream, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var text = new StringBuilder();
        using var wordDocument = WordprocessingDocument.Open(memoryStream, false);

        var body = wordDocument.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }
        }

        return text.ToString();
    }

    private async Task<string> ExtractFromExcelAsync(Stream stream, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var text = new StringBuilder();

        try
        {
            using var workbook = new XLWorkbook(memoryStream);

            foreach (var worksheet in workbook.Worksheets)
            {
                text.AppendLine($"=== {worksheet.Name} ===");
                text.AppendLine();

                // Obter o range usado (células com conteúdo)
                var usedRange = worksheet.RangeUsed();

                if (usedRange != null)
                {
                    // Iterar por todas as linhas do range usado
                    foreach (var row in usedRange.Rows())
                    {
                        var rowTexts = new List<string>();

                        foreach (var cell in row.Cells())
                        {
                            // Obter o valor da célula como string
                            var cellValue = cell.GetValue<string>();

                            if (!string.IsNullOrWhiteSpace(cellValue))
                            {
                                rowTexts.Add(cellValue.Trim());
                            }
                        }

                        if (rowTexts.Count > 0)
                        {
                            text.AppendLine(string.Join(" | ", rowTexts));
                        }
                    }
                }
                else
                {
                    text.AppendLine("(Planilha vazia)");
                }

                text.AppendLine();
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao processar arquivo Excel: {ex.Message}", ex);
        }
    }

    private async Task<string> ExtractFromTxtAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Limpa o texto extraído removendo espaços em branco excessivos
    /// </summary>
    private static string CleanExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove linhas em branco excessivas mantendo a estrutura
        var lines = text.Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();

        // Remove linhas completamente vazias consecutivas (mantém no máximo uma linha vazia)
        var result = new List<string>();
        bool previousWasEmpty = false;

        foreach (var line in lines)
        {
            bool currentIsEmpty = string.IsNullOrWhiteSpace(line);

            if (!currentIsEmpty || !previousWasEmpty)
            {
                result.Add(line);
            }

            previousWasEmpty = currentIsEmpty;
        }

        return string.Join("\n", result).Trim();
    }
}
