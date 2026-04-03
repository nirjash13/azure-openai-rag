using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using UglyToad.PdfPig;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>Extracts text from PDF files using UglyToad.PdfPig.</summary>
public sealed class PdfDocumentParser : IDocumentParser
{
    private readonly ILogger<PdfDocumentParser> _logger;

    public PdfDocumentParser(ILogger<PdfDocumentParser> logger)
    {
        _logger = logger;
    }

    public DocumentType SupportedType => DocumentType.Pdf;

    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(ms);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.Append(page.Text);
            sb.AppendLine();
        }

        _logger.LogDebug("Extracted text from PDF: {PageCount} pages", pdf.NumberOfPages);
        return sb.ToString().Trim();
    }
}
