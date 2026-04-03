using AzureAI.Extraction.Models;

namespace AzureAI.Extraction.Services;

public sealed class AccountMappingService
{
    private static readonly Dictionary<string, AccountMapping> _mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["software"]   = new("software",   "6100", "Software & Subscriptions"),
        ["hardware"]   = new("hardware",   "6200", "Computer Hardware"),
        ["travel"]     = new("travel",     "6300", "Travel & Entertainment"),
        ["office"]     = new("office",     "6400", "Office Supplies"),
        ["consulting"] = new("consulting", "6500", "Professional Services"),
        ["utilities"]  = new("utilities",  "6600", "Utilities"),
    };

    public AccountMapping? GetMapping(string category) =>
        _mappings.TryGetValue(category, out var m) ? m : null;

    public LedgerEntry ToLedgerEntry(ExtractedInvoice invoice, string category)
    {
        var mapping = GetMapping(category)
            ?? new AccountMapping(category, "9999", "Uncategorized");

        return new LedgerEntry(
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            $"{invoice.VendorName} - Invoice {invoice.InvoiceNumber}",
            invoice.TotalAmount,
            mapping.AccountCode,
            mapping.AccountName,
            invoice.Currency);
    }
}
