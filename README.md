# Azure OpenAI RAG

A production-ready Retrieval-Augmented Generation (RAG) API built with .NET 8 and Azure AI services. Upload documents, ask questions grounded in their content, run semantic search, and use an AI agent that can invoke tools — all through a clean REST API.

## What it does

| Capability | Description |
|---|---|
| **Document ingestion** | Upload PDF, DOCX, Markdown, HTML, or plain text files. The API parses, chunks, embeds, and indexes them automatically. |
| **RAG Q&A** | Ask a question and receive an answer grounded in your uploaded documents, with source citations. |
| **Semantic search** | Vector similarity search across all indexed content. |
| **Multi-turn chat** | Maintain conversation history across multiple questions. |
| **AI agent (function calling)** | An agentic loop that can call registered tools (financial summaries, document search, expense entries, weather) to answer complex queries. |
| **Structured extraction** | Extract structured data (invoices, receipts) from documents and map them to ledger entries. |
| **Predictive analytics** | Forecast spending, detect anomalies, and get budget recommendations powered by GPT-4o. |

## Architecture

```
AzureAI.Api              ← Minimal API endpoints, middleware, health checks
AzureAI.Application      ← MediatR commands/queries, validation, pipeline behaviors
AzureAI.Core             ← Domain entities, value objects, interfaces (no dependencies)
AzureAI.Infrastructure   ← Azure OpenAI, Azure AI Search, EF Core (PostgreSQL), document parsers
AzureAI.FunctionCalling  ← Tool registry, function-calling orchestrator
AzureAI.Extraction       ← Invoice/receipt extraction services
```

Clean Architecture: `Core` has zero external dependencies. `Infrastructure` implements `Core` interfaces. `Application` orchestrates use cases. `Api` is the composition root.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL + Seq)
- An **Azure OpenAI** resource with two deployed models:
  - A chat model — `gpt-4o` (recommended)
  - An embedding model — `text-embedding-3-large` (recommended, 3072 dimensions)
- An **Azure AI Search** resource (Basic tier or higher for vector search)

## Quick start

### 1. Clone and configure

```bash
git clone https://github.com/nirjash13/azure-openai-rag.git
cd azure-openai-rag
cp .env.example .env
```

Edit `.env` and fill in your Azure credentials:

```env
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_SEARCH_ENDPOINT=https://your-search.search.windows.net
AZURE_SEARCH_API_KEY=your-search-api-key
```

### 2. Start dependencies (PostgreSQL + Seq)

```bash
docker compose up postgres seq -d
```

### 3. Run the API

```bash
dotnet run --project src/AzureAI.Api
```

The API starts at `http://localhost:5100`. On first run it:
- Applies database migrations automatically
- Creates the Azure AI Search index if it doesn't exist

Swagger UI is available at `http://localhost:5100/swagger` in Development mode.

### 4. Or run everything with Docker

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| API | http://localhost:5100 |
| Swagger | http://localhost:5100/swagger |
| Seq logs | http://localhost:5341 |

## Configuration

All settings live in `src/AzureAI.Api/appsettings.json`. Key sections:

```json
{
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "ChatDeployment": "gpt-4o",
    "EmbeddingDeployment": "text-embedding-3-large",
    "MaxTokens": 2000,
    "Temperature": 0.7
  },
  "AzureSearch": {
    "Endpoint": "",
    "ApiKey": "",
    "IndexName": "azure-ai-rag-index",
    "TopK": 5,
    "MinScore": 0.5
  },
  "Chunking": {
    "ChunkSizeTokens": 512,
    "OverlapTokens": 64,
    "MaxChunksPerDocument": 500
  },
  "Rag": {
    "MaxContextTokens": 8000,
    "MaxConversationHistory": 10,
    "IncludeCitations": true
  }
}
```

In production, supply secrets via environment variables using the `__` separator (e.g. `AzureOpenAI__ApiKey=...`). Never commit real keys.

## API reference

### Documents

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/documents` | Upload a document (`multipart/form-data`) |
| `GET` | `/api/v1/documents` | List all documents |
| `GET` | `/api/v1/documents/{id}` | Get a document by ID |
| `DELETE` | `/api/v1/documents/{id}` | Delete a document and its index entries |

**Upload a document:**
```bash
curl -X POST http://localhost:5100/api/v1/documents \
  -F "file=@report.pdf;type=application/pdf"
```

### Chat (RAG Q&A)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/chat` | Ask a question; get a grounded answer |
| `POST` | `/api/v1/chat/conversations` | Create a conversation session |
| `GET` | `/api/v1/chat/{conversationId}` | Get conversation history |
| `DELETE` | `/api/v1/chat/{conversationId}` | Delete a conversation |

**Ask a question:**
```bash
curl -X POST http://localhost:5100/api/v1/chat \
  -H "Content-Type: application/json" \
  -d '{
    "question": "What were the key findings in Q3?",
    "conversationId": null,
    "options": { "topK": 5, "includeCitations": true }
  }'
```

**Multi-turn conversation:**
```bash
# 1. Create a conversation
curl -X POST http://localhost:5100/api/v1/chat/conversations \
  -H "Content-Type: application/json" \
  -d '{"title": "Q3 Review"}'

# 2. Ask questions using the returned conversationId
curl -X POST http://localhost:5100/api/v1/chat \
  -H "Content-Type: application/json" \
  -d '{"question": "Summarise the risks", "conversationId": "<id>"}'
```

### Search

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/search` | Semantic vector search without generating a completion |

```bash
curl -X POST http://localhost:5100/api/v1/search \
  -H "Content-Type: application/json" \
  -d '{"query": "budget overrun", "topK": 10}'
```

### Agent (function calling)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/agent/chat` | Send a message to the AI agent; it can call tools to answer |
| `GET` | `/api/v1/agent/tools` | List all registered tools |

```bash
curl -X POST http://localhost:5100/api/v1/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What is the financial summary for Q3 and what is the weather in London?"}'
```

### Extraction

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/extract/invoice` | Extract structured data from an invoice image or document |
| `POST` | `/api/v1/extract/receipt` | Extract structured data from a receipt |
| `POST` | `/api/v1/extract/to-ledger-entry` | Convert extracted invoice data to a ledger entry |

### Analytics

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/analytics/forecast` | Forecast future spending based on historical data |
| `POST` | `/api/v1/analytics/anomalies` | Detect anomalous transactions using z-score analysis |
| `POST` | `/api/v1/analytics/recommendations` | Get AI-powered budget recommendations |

### Health

```bash
curl http://localhost:5100/health
```

Returns the status of PostgreSQL, Azure OpenAI, and Azure AI Search.

## Running tests

```bash
dotnet test
```

101 tests across 5 assemblies — unit tests for domain logic, application handlers, and integration tests with a mock-backed `WebApplicationFactory`.

```
AzureAI.Core.Tests          36 tests  — domain entities, value objects
AzureAI.Application.Tests   24 tests  — command/query handlers, behaviors, analytics
AzureAI.Extraction.Tests    16 tests  — invoice/receipt extraction logic
AzureAI.FunctionCalling.Tests 13 tests — tool registry, orchestrator, security
AzureAI.Integration.Tests   12 tests  — HTTP endpoint contracts
```

## Project structure

```
azure-openai-rag/
├── src/
│   ├── AzureAI.Api/                    # Web API (entry point)
│   │   ├── Endpoints/                  # Minimal API route handlers
│   │   ├── HealthChecks/               # Azure OpenAI + Search health checks
│   │   └── Middleware/                 # Exception handler, correlation ID, request logging
│   ├── AzureAI.Application/            # Use cases (MediatR)
│   │   ├── Commands/                   # IngestDocument, DeleteDocument, CreateConversation
│   │   ├── Queries/                    # AskQuestion, SemanticSearch, Analytics
│   │   └── Behaviors/                  # Validation, logging, performance pipeline
│   ├── AzureAI.Core/                   # Domain model (no dependencies)
│   │   ├── Domain/Entities/            # Document, DocumentChunk, ConversationSession
│   │   ├── Domain/ValueObjects/        # EmbeddingVector, ChunkMetadata, TokenUsage
│   │   └── Interfaces/                 # Repository and service contracts
│   ├── AzureAI.Infrastructure/         # External service implementations
│   │   ├── AzureOpenAI/                # Embedding + completion services
│   │   ├── Search/                     # Azure AI Search client, index manager
│   │   ├── DocumentProcessing/         # PDF (PdfPig), DOCX, HTML, Markdown parsers
│   │   └── Persistence/                # EF Core DbContext, repositories, migrations
│   ├── AzureAI.FunctionCalling/        # AI agent with tool registry
│   └── AzureAI.Extraction/             # Structured data extraction
└── tests/
    ├── AzureAI.Core.Tests/
    ├── AzureAI.Application.Tests/
    ├── AzureAI.Extraction.Tests/
    ├── AzureAI.FunctionCalling.Tests/
    └── AzureAI.Integration.Tests/
```

## Supported document formats

| Format | Parser | Notes |
|---|---|---|
| PDF | UglyToad.PdfPig | Full text extraction per page |
| DOCX | ZIP + XML | Paragraph boundaries preserved |
| Markdown | Custom | Headings, links, code blocks stripped to plain text |
| HTML | Regex | Scripts, styles, and tags removed; entities decoded |
| Plain text | StreamReader | Raw UTF-8 content |

## Key design decisions

- **Clean Architecture** — domain has zero external dependencies; infrastructure is pluggable
- **MediatR CQRS** — all use cases are commands or queries with pipeline behaviors for validation, logging, and performance monitoring
- **Sliding window chunking** — overlapping chunks preserve cross-boundary context; token counts use `TiktokenTokenizer` (GPT-4o tokeniser)
- **Polly resilience** — retry (exponential back-off + jitter) and circuit-breaker policies on all Azure HTTP calls
- **Serilog structured logging** — console + Seq sink; correlation ID propagated on every request
- **EF Core + PostgreSQL** — document metadata and conversation history persisted relationally; embeddings live in Azure AI Search

## Troubleshooting

**`dotnet run` fails with Azure credential errors**
Fill in `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureSearch:Endpoint`, and `AzureSearch:ApiKey` in `appsettings.Development.json` (not committed) or environment variables.

**`/health` returns 503**
One or more Azure services is unreachable. The API still starts — 503 from `/health` just means the background checks are failing. Check the Seq logs at `http://localhost:5341` for details.

**Postgres migration error on startup**
Ensure PostgreSQL is running (`docker compose up postgres -d`) before starting the API. The API applies migrations automatically but needs a reachable database.

**PDF text extraction returns empty content**
Confirm the PDF contains selectable text (not a scanned image). Scanned PDFs require an OCR step (not included).
