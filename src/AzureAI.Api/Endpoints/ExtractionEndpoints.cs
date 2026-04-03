using System.Text.Json;
using AzureAI.Api.Models;
using AzureAI.Extraction.Models;
using AzureAI.Extraction.Services;

namespace AzureAI.Api.Endpoints;

internal static class ExtractionEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static IEndpointRouteBuilder MapExtractionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/extract")
            .WithTags("Extraction")
            .WithOpenApi();

        group.MapPost("/invoice", ExtractInvoice)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces<ApiResponse<ExtractionResult<ExtractedInvoice>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        group.MapPost("/receipt", ExtractReceipt)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces<ApiResponse<ExtractionResult<ExtractedReceipt>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        group.MapPost("/to-ledger-entry", ToLedgerEntry)
            .Produces<ApiResponse<LedgerEntry>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> ExtractInvoice(
        IFormFile file,
        InvoiceExtractionService extractionService,
        ExtractionValidationService validationService,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(ApiResponse.Fail("File is required."));

        var result      = await extractionService.ExtractAsync(file.OpenReadStream(), file.ContentType, ct);
        var validations = validationService.ValidateInvoice(result.Data);
        var hasLow      = validationService.HasLowConfidenceFields(validations);

        var enriched = result with
        {
            FieldConfidences      = validations,
            HasLowConfidenceFields = hasLow,
        };

        return Results.Ok(ApiResponse.Ok(enriched));
    }

    private static async Task<IResult> ExtractReceipt(
        IFormFile file,
        ReceiptExtractionService extractionService,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(ApiResponse.Fail("File is required."));

        var result = await extractionService.ExtractAsync(file.OpenReadStream(), file.ContentType, ct);
        return Results.Ok(ApiResponse.Ok(result));
    }

    private static IResult ToLedgerEntry(
        LedgerEntryRequest request,
        AccountMappingService mappingService)
    {
        if (string.IsNullOrWhiteSpace(request.InvoiceJson))
            return Results.BadRequest(ApiResponse.Fail("invoiceJson is required."));

        if (string.IsNullOrWhiteSpace(request.Category))
            return Results.BadRequest(ApiResponse.Fail("category is required."));

        ExtractedInvoice? invoice;
        try
        {
            invoice = JsonSerializer.Deserialize<ExtractedInvoice>(request.InvoiceJson, _jsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(ApiResponse.Fail("invoiceJson is not valid JSON."));
        }

        if (invoice is null)
            return Results.BadRequest(ApiResponse.Fail("invoiceJson could not be deserialized."));

        var entry = mappingService.ToLedgerEntry(invoice, request.Category);
        return Results.Ok(ApiResponse.Ok(entry));
    }
}

internal sealed record LedgerEntryRequest(string InvoiceJson, string Category);
