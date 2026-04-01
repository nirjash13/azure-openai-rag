namespace AzureAI.Core.Domain.Enums;

/// <summary>Tracks the processing lifecycle of a document.</summary>
public enum IngestionStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
