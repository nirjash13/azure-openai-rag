using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>Extracts text from Markdown files by reading the raw content of the stream.</summary>
public sealed class MarkdownParser : IDocumentParser
{
    /// <inheritdoc />
    public DocumentType SupportedType => DocumentType.Markdown;

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
