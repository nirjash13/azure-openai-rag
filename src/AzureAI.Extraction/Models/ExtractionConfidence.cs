namespace AzureAI.Extraction.Models;

public sealed record ExtractionConfidence(
    string FieldName,
    double Score,
    string? Reason = null);
