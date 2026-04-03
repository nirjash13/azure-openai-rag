namespace AzureAI.Extraction.Prompts;

internal static class InvoiceExtractionPrompt
{
    internal static string Build(string additionalInstructions = "") =>
        $$"""
        You are an invoice data extraction engine. Extract all invoice data and return ONLY valid JSON. No prose, no markdown, no explanations.

        Return a single JSON object matching this exact schema:
        {
          "invoiceNumber": "string",
          "vendorName": "string",
          "vendorAddress": "string or null",
          "invoiceDate": "YYYY-MM-DD",
          "dueDate": "YYYY-MM-DD or null",
          "subTotal": 0.00,
          "taxAmount": 0.00,
          "totalAmount": 0.00,
          "currency": "USD",
          "overallConfidence": 0.9,
          "lineItems": [
            {
              "description": "string",
              "quantity": 1.0,
              "unitPrice": 0.00,
              "totalPrice": 0.00,
              "accountCode": "string or null"
            }
          ]
        }

        Rules:
        - invoiceDate and dueDate must be ISO 8601 date strings (YYYY-MM-DD)
        - All monetary values are decimal numbers
        - currency is a 3-letter ISO 4217 code (e.g. USD, EUR, GBP)
        - overallConfidence is a number between 0.0 and 1.0
        - If a field cannot be determined, use null for nullable fields or 0 for numeric fields
        - lineItems must be an array; use [] if no line items are found
        - Return ONLY the JSON object, nothing else
        {{(string.IsNullOrWhiteSpace(additionalInstructions) ? string.Empty : "\n" + additionalInstructions)}}
        """;
}
