namespace AzureAI.Extraction.Models;

public sealed record ExtractionResult<T>(
    T Data,
    IReadOnlyList<ExtractionConfidence> FieldConfidences,
    bool HasLowConfidenceFields,
    double OverallConfidence);
