using AzureAI.Extraction.Models;
using AzureAI.Extraction.Services;
using FluentAssertions;

namespace AzureAI.Extraction.Tests;

public sealed class ExtractionValidationServiceTests
{
    private readonly ExtractionValidationService _sut = new();

    private static ExtractedInvoice MakeInvoice(
        string invoiceNumber = "INV-001",
        decimal subTotal     = 100m,
        decimal taxAmount    = 10m,
        decimal totalAmount  = 110m,
        IReadOnlyList<ExtractedLineItem>? lineItems = null) =>
        new(
            invoiceNumber,
            "Acme Corp",
            null,
            new DateOnly(2024, 1, 15),
            null,
            subTotal,
            taxAmount,
            totalAmount,
            "USD",
            lineItems ?? [new ExtractedLineItem("Widget", 1, 100m, 100m)],
            0.9);

    [Fact]
    public void ValidateInvoice_missing_invoice_number_produces_low_confidence_on_that_field()
    {
        var invoice = MakeInvoice(invoiceNumber: "");

        var result = _sut.ValidateInvoice(invoice);

        var invoiceNumberConf = result.Single(c => c.FieldName == "InvoiceNumber");
        invoiceNumberConf.Score.Should().Be(0.0);
        invoiceNumberConf.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateInvoice_valid_invoice_with_matching_totals_has_no_low_confidence_fields()
    {
        var invoice = MakeInvoice(subTotal: 100m, taxAmount: 10m, totalAmount: 110m);

        var result  = _sut.ValidateInvoice(invoice);
        var hasLow  = _sut.HasLowConfidenceFields(result);

        hasLow.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvoice_total_mismatch_produces_low_confidence_on_TotalsCheck()
    {
        var invoice = MakeInvoice(subTotal: 100m, taxAmount: 10m, totalAmount: 200m);

        var result = _sut.ValidateInvoice(invoice);

        var totalsConf = result.Single(c => c.FieldName == "TotalsCheck");
        totalsConf.Score.Should().BeLessThan(0.7);
    }

    [Fact]
    public void ValidateInvoice_no_line_items_produces_low_confidence_on_LineItems()
    {
        var invoice = MakeInvoice(lineItems: Array.Empty<ExtractedLineItem>());

        var result = _sut.ValidateInvoice(invoice);

        var lineItemsConf = result.Single(c => c.FieldName == "LineItems");
        lineItemsConf.Score.Should().BeLessThan(0.7);
    }

    [Fact]
    public void HasLowConfidenceFields_returns_true_when_any_score_below_threshold()
    {
        var confidences = new[]
        {
            new ExtractionConfidence("FieldA", 0.9),
            new ExtractionConfidence("FieldB", 0.5),
        };

        _sut.HasLowConfidenceFields(confidences).Should().BeTrue();
    }

    [Fact]
    public void HasLowConfidenceFields_returns_false_when_all_scores_at_or_above_threshold()
    {
        var confidences = new[]
        {
            new ExtractionConfidence("FieldA", 0.9),
            new ExtractionConfidence("FieldB", 0.7),
        };

        _sut.HasLowConfidenceFields(confidences).Should().BeFalse();
    }
}
