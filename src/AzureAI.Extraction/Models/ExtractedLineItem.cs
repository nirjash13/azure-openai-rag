namespace AzureAI.Extraction.Models;

public sealed record ExtractedLineItem(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? AccountCode = null);
