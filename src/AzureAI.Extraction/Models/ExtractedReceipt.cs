namespace AzureAI.Extraction.Models;

public sealed record ExtractedReceipt(
    string VendorName,
    DateOnly Date,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<string> Items);
