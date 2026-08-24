# DataImport — OFAC SDN Sanctions Platform

A .NET solution for ingesting and serving U.S. Treasury OFAC Specially
Designated Nationals (SDN) sanctions data. It's made up of two projects
sharing the same SQL Server database and domain models:

| Project | Type | Responsibility |
|---|---|---|
| **DataImport** | Console app | Downloads, parses, and upserts SDN data (ETL) |
| **DataImport.API** | ASP.NET Core Web API | Exposes sanctions data, import logs, and search via REST endpoints |

---

## 1. DataImport — Console Importer

A .NET console job that downloads the OFAC SDN list, parses it, and upserts
it into SQL Server — inserting new entries, updating changed ones, and
leaving unchanged ones alone. Runs as a scheduled job via Task Scheduler.

Built as a MediatR-orchestrated pipeline of three steps: **download → parse → save**,
with every run recorded in a `DataImportLogs` table so you have a history of what
happened, when, and whether it succeeded.

### How it works

```
Program.cs
  └─ ImportRunner.RunAsync
       └─ ImportOfacSdnDataCommand            (orchestrator)
            ├─ DownloadSdnXmlCommand           → gets today's sdn.xml (cache-first)
            ├─ ParseSdnXmlCommand              → XDocument → List<SanctionDetail>
            └─ SaveSanctionDetailsCommand      → batched upsert into SQL Server
```

1. **Download** — `DownloadSdnXmlCommandHandler` checks for a cached copy of
   `sdn.xml` under `Imports/yyyy-MM-dd/`. If today's cache exists, it's reused;
   otherwise the file is streamed straight from OFAC to disk (never buffered
   fully in memory) and cached for the day. Returns a `Stream` plus a
   `WasDownloaded` flag so the run history can tell cache hits from real downloads.

2. **Parse** — `ParseSdnXmlCommandHandler` reads the cached file as a `Stream`
   (via `XDocument.Load`, not `Parse`, so the whole file is never materialized
   as one in-memory string) and produces one `SanctionDetail` per `<sdnEntry>`.

3. **Save** — `SaveSanctionDetailsCommandHandler` loads all existing records
   with `AsNoTracking()`, compares each parsed record against them, and
   batches inserts/updates in groups of 500 — calling `ChangeTracker.Clear()`
   after each batch so change-tracking cost stays constant instead of growing
   across the whole run.

4. **Log** — `ImportOfacSdnDataCommandHandler` wraps all three steps in a
   try/catch/finally and always writes a `DataImportLog` row — even on
   failure — capturing counts, whether the source was downloaded or cached,
   success/failure, and the error message if any.

5. **Cache cleanup** — stale day-partitioned cache folders are cleaned up
   automatically so `Imports/` doesn't grow unbounded across repeated runs.

### Project structure

| Folder | Contents |
|---|---|
| `Commands/` | The three pipeline steps + the orchestrator (MediatR requests/handlers) |
| `Models/` | `SanctionDetail` (one row per SDN entry), `DataImportLog` (one row per run) |
| `Data/` | `SanctionsDbContext` + design-time factory for EF migrations |
| `Configuration/` | `ImportSettings` (cache folder, OFAC URL) — bound from `appsettings.json` |
| `Extensions/` | DI wiring (DbContext, HttpClient, MediatR, Serilog) |
| `Hosting/` | `ImportRunner` — builds the host, runs the import, flushes logs |
| `Logging/` | Serilog bootstrap |
| `Migrations/` | EF Core migrations |
| `DataImport.Benchmarks/` | BenchmarkDotNet project comparing SDN parsing/save approaches |

---

## 2. DataImport.API — Web App

An ASP.NET Core Web API that exposes the data captured by the importer —
sanction records, import run logs, country-level breakdowns, and fuzzy
name search — over REST endpoints. Built on the same MediatR command/query
pattern as the importer, against the same `SanctionsDbContext` / SQL Server
database. Documented via Swagger/OpenAPI for exploration and downstream
integration.

### Endpoints (current)

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/sanctions` | Paged list of sanction records (`page`, `pageSize`, `sdnType`, `lastNameContains`) |
| `GET` | `/api/sanctions/{id}` | Get a single sanction record by ID |
| `POST` | `/api/Search` | Fuzzy name search over sanction records — SQL full-text candidate retrieval, re-ranked by n-gram cosine similarity (body: `{ "name": string }`) |
| `POST` | `/api/Country/get-count-by-country` | Count of sanctioned records for a given country |
| `POST` | `/api/Country/get-sanctions-by-country` | Paged list of sanction records for a given country (`page`, `pageSize`) |
| `GET` | `/api/logger` | Paged list of import runs (`page`, `pageSize`, `orderByDescending`) |
| `GET` | `/api/logger/error-count` | Count of failed import runs |

### Project structure

| Folder | Contents |
|---|---|
| `Controllers/` | REST endpoints (`SanctionsController`, `SearchController`, `CountryController`, `LoggerController`) |
| `Queries/` | MediatR query definitions (`GetFreeTextSearchQuery`, `GetSanctionsByCountryQuery`, `GetQueriesPagedQuery`, `GetErrorCountQuery`, etc.) |
| `Commands/` | MediatR query/command handlers |
| `Presentation/GenericDTO/` | Shared response shapes (`PagedResult<T>`) and Facet-mapped DTOs |

---

## 3. Benchmarks

`DataImport.Benchmarks` uses BenchmarkDotNet to compare parsing/save strategy
approaches. Run with:
```bash
cd DataImport.Benchmarks
dotnet run -c Release
```
____

## Requirements

- .NET 10 SDK
- SQL Server (local or remote) — Developer/Express/etc. all work
- Network access to the OFAC SDN.XML endpoint (importer only)


### If you want to contribute, create descriptive pull request and let me know.
