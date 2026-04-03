using AzureAI.Extraction.Models;

namespace AzureAI.Extraction.Services;

public sealed class ExtractionValidationService
{
    private const double LowConfidenceThreshold = 0.7;

    public IReadOnlyList<ExtractionConfidence> ValidateInvoice(ExtractedInvoice invoice)
    {
        var confidences = new List<ExtractionConfidence>();

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            confidences.Add(new("InvoiceNumber", 0.0, "Missing invoice number"));
        else
            confidences.Add(new("InvoiceNumber", 0.95));

        if (invoice.TotalAmount <= 0)
            confidences.Add(new("TotalAmount", 0.1, "Total amount is zero or negative"));
        else
            confidences.Add(new("TotalAmount", 0.95));

        var lineItemConfidence = invoice.LineItems.Count == 0
            ? new ExtractionConfidence("LineItems", 0.3, "No line items extracted")
            : new ExtractionConfidence("LineItems", 0.9);
        confidences.Add(lineItemConfidence);

        var expectedTotal = invoice.SubTotal + invoice.TaxAmount;
        var tolerance     = Math.Abs(expectedTotal * 0.01m);
        if (Math.Abs(expectedTotal - invoice.TotalAmount) > tolerance)
            confidences.Add(new("TotalsCheck", 0.4, $"Total mismatch: {invoice.SubTotal}+{invoice.TaxAmount}≠{invoice.TotalAmount}"));

        return confidences;
    }

    public bool HasLowConfidenceFields(IReadOnlyList<ExtractionConfidence> confidences) =>
        confidences.Any(c => c.Score < LowConfidenceThreshold);
}
