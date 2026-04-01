using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>Extracts plain text from <c>.txt</c> and <c>.md</c> files by reading the stream directly.</summary>
public sealed class PlainTextParser : IDocumentParser
{
    /// <inheritdoc />
    public DocumentType SupportedType => DocumentType.Txt;

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
