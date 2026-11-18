# DataImport

A .NET 10 ETL and sanctions-screening API. It imports the OFAC SDN list into SQL Server, keeps it in sync (including delistings), and exposes fuzzy name search for AML/KYC screening.

## Features

- **OFAC SDN import pipeline**: XML file integrity validation, then import via per-source `ISanctionImportStrategy` implementations
- **Idempotent data model**: composite unique index on `(Source, RecordUniqueId)`
- **Delisting detection**: records missing from the latest file are soft-deleted (`IsActive` / `RemovedAtUtc`), and each import log tracks a `Removed` count
- **Fuzzy name search**: SQL Server full-text candidates, re-ranked by n-gram cosine similarity with a caller-supplied `Tolerance`
- **Search term sanitizing**: tiered stripping of legal entity suffixes, generic business descriptors and honorifics before the cache key is built
- **Caching**: FusionCache
- **Resilience**: retry on failure, with email notification when an import fails
- **Scheduled full-text index rebuild**: Coravel, daily at 00:23 (API layer only)
- **Logging**: Serilog, with a logger endpoint
- **Swagger UI** for exploring the API

## Tech stack

| Area | Tech |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| Data | SQL Server, EF Core (migrations), full-text search |
| Architecture | MediatR (CQRS) |
| Search | LinqKit `PredicateBuilder`, F23.StringSimilarity |
| Caching | FusionCache |
| Scheduling | Coravel |
| Logging | Serilog |

## API

| Controller | Purpose |
|---|---|
| **Sanctions** | Get a record by id; paged list with `sdnType` and `lastNameContains` filters |
| **Search** | `POST` free-text fuzzy name search (multi-word, OR logic, scored by `Tolerance`) |
| **Country** | Count and list records by country |
| **Logger** | Read import/application logs |

Full request/response shapes are in Swagger at `/swagger` when running locally.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server with **Full-Text Search** installed
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### Setup

```bash
git clone https://github.com/TheVoid0013/DataImport.git
cd DataImport

# 1. Set your connection string (appsettings.Development.json or user-secrets)
# 2. Apply migrations (creates tables and the full-text index)
dotnet ef database update --project DataImport.API

# 3. Run
dotnet run --project DataImport.API
```

Then open `https://localhost:<port>/swagger`.

### Linux notes

Runs on Ubuntu. Watch for case-sensitive file names, SQL authentication (no Windows auth), and the `dotnet-ef` tool being on your `PATH`.

## Roadmap

- Admin dashboard (React + Vite + TypeScript, TailAdmin)
- Aggregate endpoints for dashboard charts (top countries, SDN type counts)
- GitHub wiki with full endpoint documentation

## Contributing

`master` is protected: open a pull request, no direct pushes.

## Disclaimer

This project is a personal engineering project. It is not a certified compliance product, so validate results independently before relying on them for regulatory decisions.
