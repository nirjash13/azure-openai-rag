namespace AzureAI.Extraction.Models;

public sealed record ExtractedInvoice(
    string InvoiceNumber,
    string VendorName,
    string? VendorAddress,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<ExtractedLineItem> LineItems,
    double OverallConfidence);
