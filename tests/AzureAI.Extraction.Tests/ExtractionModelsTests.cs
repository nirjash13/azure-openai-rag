using AzureAI.Extraction.Models;
using FluentAssertions;

namespace AzureAI.Extraction.Tests;

public sealed class ExtractionModelsTests
{
    [Fact]
    public void ExtractedInvoice_record_equality_compares_by_value()
    {
        var lineItems = new[] { new ExtractedLineItem("Widget", 2, 50m, 100m) };
        var date      = new DateOnly(2024, 6, 1);

        var a = new ExtractedInvoice("INV-001", "Vendor", null, date, null, 100m, 10m, 110m, "USD", lineItems, 0.9);
        var b = new ExtractedInvoice("INV-001", "Vendor", null, date, null, 100m, 10m, 110m, "USD", lineItems, 0.9);

        a.Should().Be(b);
    }

    [Fact]
    public void ExtractedInvoice_different_values_are_not_equal()
    {
        var date = new DateOnly(2024, 6, 1);
        var a    = new ExtractedInvoice("INV-001", "Vendor A", null, date, null, 100m, 10m, 110m, "USD", [], 0.9);
        var b    = new ExtractedInvoice("INV-002", "Vendor B", null, date, null, 200m, 20m, 220m, "USD", [], 0.9);

        a.Should().NotBe(b);
    }

    [Fact]
    public void LedgerEntry_properties_are_accessible()
    {
        var date  = new DateOnly(2024, 7, 15);
        var entry = new LedgerEntry("INV-100", date, "Acme - Invoice INV-100", 500m, "6100", "Software & Subscriptions", "USD");

        entry.Reference.Should().Be("INV-100");
        entry.Date.Should().Be(date);
        entry.Description.Should().Be("Acme - Invoice INV-100");
        entry.Amount.Should().Be(500m);
        entry.AccountCode.Should().Be("6100");
        entry.AccountName.Should().Be("Software & Subscriptions");
        entry.Currency.Should().Be("USD");
    }
}
