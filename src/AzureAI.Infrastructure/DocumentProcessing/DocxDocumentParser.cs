using System.IO.Compression;
using System.Text.RegularExpressions;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>
/// Extracts text from DOCX files by reading the embedded <c>word/document.xml</c> entry
/// and stripping XML tags. For production use, consider the DocumentFormat.OpenXml SDK.
/// </summary>
public sealed class DocxDocumentParser : IDocumentParser
{
    private static readonly Regex TagPattern = new(@"<[^>]+>", RegexOptions.Compiled);

    private readonly ILogger<DocxDocumentParser> _logger;

    /// <summary>Initializes a new <see cref="DocxDocumentParser"/>.</summary>
    public DocxDocumentParser(ILogger<DocxDocumentParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public DocumentType SupportedType => DocumentType.Docx;

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        _logger.LogDebug("Extracting text from DOCX via ZIP entry 'word/document.xml'");

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        using var archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
            return string.Empty;

        using var entryStream = entry.Open();
        using var reader      = new StreamReader(entryStream);
        var xml               = await reader.ReadToEndAsync(cancellationToken);

        // Strip XML tags; collapse whitespace.
        var text = TagPattern.Replace(xml, " ");
        return Regex.Replace(text, @"\s{2,}", " ").Trim();
    }
}
