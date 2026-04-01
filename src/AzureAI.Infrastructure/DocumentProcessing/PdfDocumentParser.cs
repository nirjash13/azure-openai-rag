using System.Text;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>
/// Basic PDF text extractor. Reads raw bytes and attempts to recover embedded ASCII text.
/// For production use, replace with a dedicated library such as PdfPig or iText.
/// </summary>
public sealed class PdfDocumentParser : IDocumentParser
{
    private readonly ILogger<PdfDocumentParser> _logger;

    /// <summary>Initializes a new <see cref="PdfDocumentParser"/>.</summary>
    public PdfDocumentParser(ILogger<PdfDocumentParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public DocumentType SupportedType => DocumentType.Pdf;

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        _logger.LogWarning(
            "PdfDocumentParser uses a basic byte-level text extraction. " +
            "Replace with PdfPig or iText for production-quality PDF parsing.");

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        // Extract printable ASCII runs from the binary PDF content.
        var sb          = new StringBuilder();
        var inTextRun   = false;

        foreach (var b in bytes)
        {
            if (b >= 32 && b < 127)
            {
                sb.Append((char)b);
                inTextRun = true;
            }
            else if (inTextRun)
            {
                if (sb.Length > 0 && sb[^1] != ' ')
                    sb.Append(' ');
                inTextRun = false;
            }
        }

        return sb.ToString();
    }
}
