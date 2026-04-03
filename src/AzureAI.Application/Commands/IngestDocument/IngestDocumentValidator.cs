using FluentValidation;

namespace AzureAI.Application.Commands.IngestDocument;

/// <summary>Validates an <see cref="IngestDocumentCommand"/> before the handler executes.</summary>
public sealed class IngestDocumentValidator : AbstractValidator<IngestDocumentCommand>
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "text/markdown",
        "text/html"
    };

    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB

    /// <summary>Initializes a new <see cref="IngestDocumentValidator"/> with all rules configured.</summary>
    public IngestDocumentValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(ct => SupportedContentTypes.Contains(ct))
            .WithMessage(x =>
                $"Content type '{x.ContentType}' is not supported. " +
                $"Supported types: {string.Join(", ", SupportedContentTypes)}.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required and must be readable.");
    }
}
