using AzureAI.Extraction.Models;
using AzureAI.Extraction.Services;
using FluentAssertions;

namespace AzureAI.Extraction.Tests;

public sealed class AccountMappingServiceTests
{
    private readonly AccountMappingService _sut = new();

    private static ExtractedInvoice MakeInvoice(string invoiceNumber = "INV-001", decimal total = 250m) =>
        new(
            invoiceNumber,
            "Acme Corp",
            null,
            new DateOnly(2024, 3, 1),
            null,
            200m,
            50m,
            total,
            "USD",
            [],
            0.9);

    [Fact]
    public void GetMapping_known_category_returns_correct_mapping()
    {
        var result = _sut.GetMapping("software");

        result.Should().NotBeNull();
        result!.AccountCode.Should().Be("6100");
        result.AccountName.Should().Be("Software & Subscriptions");
    }

    [Fact]
    public void GetMapping_is_case_insensitive()
    {
        var result = _sut.GetMapping("SOFTWARE");

        result.Should().NotBeNull();
        result!.AccountCode.Should().Be("6100");
    }

    [Fact]
    public void GetMapping_unknown_category_returns_null()
    {
        var result = _sut.GetMapping("unknown-category");

        result.Should().BeNull();
    }

    [Fact]
    public void ToLedgerEntry_known_category_uses_correct_account_code()
    {
        var invoice = MakeInvoice();

        var entry = _sut.ToLedgerEntry(invoice, "travel");

        entry.AccountCode.Should().Be("6300");
        entry.AccountName.Should().Be("Travel & Entertainment");
    }

    [Fact]
    public void ToLedgerEntry_unknown_category_uses_uncategorized_account()
    {
        var invoice = MakeInvoice();

        var entry = _sut.ToLedgerEntry(invoice, "mystery");

        entry.AccountCode.Should().Be("9999");
        entry.AccountName.Should().Be("Uncategorized");
    }

    [Fact]
    public void ToLedgerEntry_reference_equals_invoice_number()
    {
        var invoice = MakeInvoice(invoiceNumber: "INV-999");

        var entry = _sut.ToLedgerEntry(invoice, "office");

        entry.Reference.Should().Be("INV-999");
    }

    [Fact]
    public void ToLedgerEntry_amount_equals_total_amount()
    {
        var invoice = MakeInvoice(total: 375.50m);

        var entry = _sut.ToLedgerEntry(invoice, "hardware");

        entry.Amount.Should().Be(375.50m);
    }
}
