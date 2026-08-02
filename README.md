# DataImport — OFAC SDN Importer

A .NET console job that downloads the U.S. Treasury OFAC Specially Designated
Nationals (SDN) list, parses it, and upserts it into a SQL Server database —
inserting new entries, updating changed ones, and leaving unchanged ones alone.

Built as a MediatR-orchestrated pipeline of three steps: **download → parse → save**,
with every run recorded in a `DataImportLogs` table so you have a history of what
happened, when, and whether it succeeded.

## How it works

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

## Project structure

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

## Requirements

- .NET 10 SDK
- SQL Server (local or remote) — Developer/Express/etc. all work
- Network access to the OFAC SDN.XML endpoint

## Setup

1. **Clone and restore**
   ```bash
   git clone https://github.com/TheVoid0013/DataImport.git
   cd DataImport
   dotnet restore
   ```

2. **Configure `DataImport/appsettings.json`**
   ```json
   {
     "ConnectionStrings": {
       "SanctionsDb": "Server=localhost;Database=SanctionsImporter;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "ImportSettings": {
       "RootFolder": "Imports",
       "SdnXmlUrl": "https://www.treasury.gov/ofac/downloads/sdn.xml"
     }
   }
   ```
   - `RootFolder` can be relative (resolved against the app's base directory)
     or absolute.
   - `SdnXmlUrl` is configurable since OFAC has changed/moved this file before —
     no redeploy needed to point at a new URL.

3. **Apply migrations**
   ```bash
   cd DataImport
   dotnet ef database update
   ```

4. **Run**
   ```bash
   dotnet run
   ```

## What gets stored

**`SanctionDetails`** — one row per SDN entry, keyed by `RecordUniqueId`
(unique index), storing the raw entry XML (`XmlRecord`), extracted `Country`,
and `ImportedAtUtc`.

**`DataImportLogs`** — one row per run:

| Column | Meaning |
|---|---|
| `RanAtUtc` | When the run started |
| `Parsed` / `Inserted` / `Updated` / `Unchanged` | Result counts |
| `WasDownloaded` | `true` if fetched from OFAC this run, `false` if served from today's cache |
| `Succeeded` | Whether the run completed without throwing |
| `ErrorMessage` | Populated when `Succeeded` is `false` |

Indexed on `RanAtUtc` and on `(Succeeded, RanAtUtc)` for querying recent runs
and recent failures efficiently.

## Operational notes

- **Caching is per calendar day.** Re-running on the same day reuses the same
  cached `sdn.xml` rather than re-downloading; a new day starts a fresh cache folder.
- **Command timeout is set to 180s with retry-on-failure** (3 retries, up to
  10s delay) at the DbContext level — see `AddSanctionsDbContext` in
  `HostApplicationBuilderExtensions`. This is tuned for a modest dev/CI
  environment; adjust if your SQL Server instance is under memory pressure
  or shared with other heavy processes (see Troubleshooting below).
- **The download HttpClient sets a 5-minute timeout and a custom User-Agent** —
  treasury.gov rejects requests with no User-Agent header (403).

## Troubleshooting

- **`XmlException: Data at the root level is invalid`** — the downloaded/cached
  file isn't valid XML. Check the cached `sdn.xml` for an HTML error page or
  empty content; confirm the configured `SdnXmlUrl` is still correct.
- **SQL timeouts or "insufficient memory in the buffer pool" during the
  initial load in `SaveSanctionDetailsCommandHandler`** — this is typically
  resource contention on the machine running SQL Server (e.g. running
  low on RAM with Visual Studio, browser, SSMS all open simultaneously),
  not a code or hardware defect. Check `sys.dm_os_sys_memory` and available
  RAM; consider running the job outside the debugger for scheduled/production runs.
- **Updates seem to run but don't persist** — make sure any record fetched
  for comparison via `AsNoTracking()` is explicitly `Attach`ed and marked
  `EntityState.Modified` before `SaveChangesAsync()`; an untracked entity's
  mutated properties won't generate an `UPDATE` on their own.

## Benchmarks

`DataImport.Benchmarks` uses BenchmarkDotNet to compare parsing/save strategy
approaches. Run with:
```bash
cd DataImport.Benchmarks
dotnet run -c Release
```
____
### If you want to contribute, create descriptive pull request and let me know.
