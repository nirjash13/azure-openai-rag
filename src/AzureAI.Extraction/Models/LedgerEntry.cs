namespace AzureAI.Extraction.Models;

public sealed record LedgerEntry(
    string Reference,
    DateOnly Date,
    string Description,
    decimal Amount,
    string AccountCode,
    string AccountName,
    string Currency);
