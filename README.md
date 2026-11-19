# DataImport

[![Build](https://github.com/TheVoid0013/DataImport/actions/workflows/build.yml/badge.svg)](https://github.com/TheVoid0013/DataImport/actions/workflows/build.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

DataImport loads the U.S. Treasury **OFAC Specially Designated Nationals (SDN) list** into SQL Server, keeps it in sync from day to day (including delistings), and exposes it through a versioned REST API with fuzzy name search.

It has two parts:

- **Importer** (`DataImport`): a console job that downloads, validates, parses and upserts the SDN list. It runs once and exits, so you schedule it yourself.
- **API** (`DataImport.API`): an ASP.NET Core Web API for looking up records, searching names and reading statistics and import history.

> **Disclaimer:** This is a personal engineering project, not a certified compliance product. Do not rely on it for regulatory screening decisions without independent validation. See [Known limitations](#known-limitations).

---

## Contents

- [How it works](#how-it-works)
- [Solution layout](#solution-layout)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Running the importer](#running-the-importer)
- [API reference](#api-reference)
- [Data model](#data-model)
- [Testing and benchmarks](#testing-and-benchmarks)
- [Known limitations](#known-limitations)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## How it works

### Import pipeline

Each run is one `ImportOfacSdnDataCommand`, made of MediatR steps in this order:

```
CleanupImportCache ─► DownloadSdnXml ─► LoadAndValidateSdnXml ─► ParseSdnXml ─► SaveSanctionDetails
                                                                                       │
                                                         DataImportLogs row ◄──────────┘ (always written)
                                                         Failure email      ◄── on error
```

1. **Clean up the cache.** Deletes cached download folders older than `CacheRetentionDays`. If this step fails, the import logs a warning and carries on.
2. **Download.** Fetches `sdn.xml` from OFAC and streams it straight to disk under `RootFolder/yyyy-MM-dd/`. If today's file is already there, it is reused instead of downloaded again.
3. **Validate.** Rejects the file if it is smaller than 5 MB, is not well-formed XML, has an unexpected root namespace, or contains fewer than 1,000 `sdnEntry` elements.
4. **Parse.** Builds one `SanctionDetail` per `sdnEntry`, keeping the uid, SDN type, last name, first name, the first address's country and the raw entry XML. **Any** malformed entry (a missing uid, a duplicate uid, or a missing last name or SDN type) fails the whole import. A silently skipped entry would otherwise look like a delisting in the next step.
5. **Save (upsert and delist):**
   - Loads the existing records and works out which ones would be delisted **before writing anything**.
   - Aborts if more records would be delisted than `Import:MaxRemovalsPerRun` allows (default 500). A truncated file looks the same as a mass delisting.
   - Inserts new records, updates changed ones, leaves unchanged ones alone and reactivates relisted ones, in batches of 500.
   - Soft-deletes records that are no longer in the file by setting `IsActive = false` and `RemovedAtUtc`.

Every run writes a row to `DataImportLogs` with its counts, whether it succeeded and any error message. If a run fails, an alert is emailed over SMTP. A failed email never fails the import.

The runner tries an import **up to 3 times, 15 minutes apart**. SQL Server calls also use EF Core's transient-fault retry. The process exits with code `0` on success and `1` on failure, so schedulers and monitoring can act on the result.

### Name search

`POST /api/v1/Search` works like this:

1. **Sanitizes** the input. It strips punctuation, then removes legal suffixes (`ltd`, `llc`, `gmbh`, …) and personal titles (`mr`, `dr`, `sheikh`, …). Generic business words (`bank`, `trading`, `holdings`, …) are removed only if other words remain.
2. **Finds candidates** with SQL Server full-text search (`FREETEXT`) on `FirstName` and `LastName`, ORing the remaining words together. Candidates are cached for 5 minutes in FusionCache, keyed on the sanitized words.
3. **Re-ranks** the candidates by character-bigram cosine similarity against the original input and drops anything below the tolerance threshold. Results come back sorted by `MatchScore` (0–100), with the distinct SDN types and countries among them.

---

## Solution layout

| Project | Type | Purpose |
|---|---|---|
| `DataImport` | Console app | The import job: pipeline commands, retry runner, SMTP notifier, Serilog setup |
| `DataImport.API` | ASP.NET Core Web API | Controllers, API versioning, Swagger, enum validation filter |
| `DataImport.Commands` | Class library | MediatR query handlers used by the API, search-term sanitizer, similarity scorer |
| `DataImport.Data` | Class library | `SanctionsDbContext`, entities, EF Core migrations (including the full-text index) |
| `DataImport.Presentation` | Class library | DTOs (generated with Facet) |
| `DataImport.Tests` | xUnit | Tests for the importer (retry runner, host setup, timing logger) |
| `DataImport.API.Tests` | xUnit | Tests for the controllers, enum filter and database configuration |
| `DataImport.Benchmarks` | BenchmarkDotNet | Benchmarks comparing XML parsing approaches |

---

## Tech stack

| Area | Tech |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| Data | SQL Server (with Full-Text Search), EF Core 10 |
| Architecture | MediatR (CQRS-style commands and queries) |
| Search | LinqKit `PredicateBuilder`, F23.StringSimilarity (cosine, n = 2) |
| Mapping | Facet |
| Caching | FusionCache (in-memory) |
| API | Asp.Versioning, Swashbuckle |
| Logging | Serilog (console and daily rolling file), MethodTimer.Fody |
| Testing | xUnit, NSubstitute, BenchmarkDotNet |
| CI | GitHub Actions: restore, build and test on every push and PR to `master` |

Package versions are managed centrally in `Directory.Packages.props`.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019 or later with the **Full-Text Search** feature installed. On Linux, install the `mssql-server-fts` package. The `mcr.microsoft.com/mssql/server` Docker image does **not** include it.
- The EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Clone

```bash
git clone https://github.com/TheVoid0013/DataImport.git
cd DataImport
```

### 2. Set the connection string

Both the importer and the API read `ConnectionStrings:SanctionsDb`, and both refuse to start without it. Keep it out of source control by using user-secrets:

```bash
CONN="Server=localhost;Database=Sanctions;User Id=sa;Password=<your-password>;TrustServerCertificate=True"

dotnet user-secrets set "ConnectionStrings:SanctionsDb" "$CONN" --project DataImport.API
dotnet user-secrets set "ConnectionStrings:SanctionsDb" "$CONN" --project DataImport
```

Or set the `ConnectionStrings__SanctionsDb` environment variable.

### 3. Create the database

The migrations live in `DataImport.Data`. They create the tables, the indexes, and the full-text catalog and index.

```bash
dotnet ef database update \
  --project DataImport.Data \
  --startup-project DataImport.API \
  --connection "$CONN"
```

> The design-time `SanctionsDbContextFactory` reads only `appsettings*.json`, not user-secrets or environment variables, which is why `--connection` is passed explicitly here.

### 4. Load the data

```bash
dotnet run --project DataImport
```

The first run downloads the full list (about 18,000 entries) and takes a minute or two.

### 5. Start the API

```bash
dotnet run --project DataImport.API
```

Open <http://localhost:5049/swagger> (or `https://localhost:7171/swagger`). Swagger is enabled only in the `Development` environment.

### Linux notes

The project runs on Ubuntu. File names are case-sensitive, Windows authentication isn't available (use SQL authentication), and `~/.dotnet/tools` must be on your `PATH` for `dotnet-ef`.

---

## Configuration

### Importer (`DataImport/appsettings.json`)

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:SanctionsDb` | — (required) | SQL Server connection string |
| `ImportSettings:RootFolder` | `Imports` | Where downloads are cached, one `yyyy-MM-dd` folder per day. Relative paths resolve against the app's base directory. |
| `ImportSettings:SdnXmlUrl` | `https://www.treasury.gov/ofac/downloads/sdn.xml` | Source URL. Configurable in case OFAC moves the file. |
| `ImportSettings:CacheRetentionDays` | `14` | Days of cached downloads to keep. `0` or less turns cleanup off. |
| `Import:MaxRemovalsPerRun` | `500` | Safety limit on delistings per run. If a large legitimate delisting happens, check it against OFAC Recent Actions, raise this value and rerun. |
| `Email:Smtp:Host` / `Port` | — | SMTP server (SSL is always on) |
| `Email:Smtp:User` / `AppPassword` | — | SMTP credentials. **Use user-secrets.** |
| `Email:Smtp:From` | — | Sender address |
| `Email:Smtp:To` | `[]` | List of recipients for failure alerts |

Example `Email` section, best kept in user-secrets:

```json
{
  "Email": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "From": "alerts@example.com",
      "User": "alerts@example.com",
      "AppPassword": "<app-password>",
      "To": [ "ops@example.com" ]
    }
  }
}
```

Importer logs go to the console and to `Logs/import-YYYYMMDD.log` next to the binary. Log files are kept for 30 days.

### API (`DataImport.API/appsettings.json`)

| Key | Description |
|---|---|
| `ConnectionStrings:SanctionsDb` | SQL Server connection string (required) |

---

## Running the importer

The importer runs once and exits; it doesn't schedule itself. OFAC publishes updates on business days, so a daily run is usually enough. Allow up to about 45 minutes for the worst case of three attempts.

**Linux (cron):**

```cron
# 02:30 every day
30 2 * * * cd /opt/dataimport && ./DataImport >> /var/log/dataimport-cron.log 2>&1
```

**Windows:** create a Task Scheduler task that runs `DataImport.exe` and alerts on a non-zero exit code.

To publish a self-contained build:

```bash
dotnet publish DataImport -c Release -o ./publish/importer
```

---

## API reference

All routes are versioned under `/api/v1/`. When no version is given, v1 is assumed. Paged endpoints take `page` (default 1) and `pageSize` (default 20). Some endpoints limit `pageSize` to 100.

### Sanctions

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/sanctions/{uid}` | Get one record by its OFAC uid (`RecordUniqueId`). Returns 404 if it isn't found. |
| `GET` | `/api/v1/sanctions` | Paged list of active records. Optional filters: `sdnType`, `lastNameContains`. |
| `GET` | `/api/v1/sanctions/Paged-delisted-Sanctions` | Paged list of delisted records, with the same filters |

### Search

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/Search` | Fuzzy name search |

```http
POST /api/v1/Search
Content-Type: application/json

{ "name": "Banco Nacional de Cuba" }
```

```json
{
  "totalCount": 1,
  "distinctSdnTypes": [ "Entity" ],
  "distinctCountries": [ "Cuba" ],
  "results": [
    {
      "recordUniqueId": "306",
      "lastName": "BANCO NACIONAL DE CUBA",
      "firstName": null,
      "sdnType": "Entity",
      "country": "Cuba",
      "isActive": true,
      "xmlRecord": "<sdnEntry>…</sdnEntry>",
      "matchScore": 100.0
    }
  ]
}
```

*The response above shows the shape only. Each result carries every `SanctionDetail` column except `Id`, plus `matchScore`.*

### Country

`country` is a value of the `Country` enum (for example `Cuba` or `Iran`). Invalid values return `400` with `ValidationProblemDetails`.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/Country/get-count-by-country?country=` | Number of records for a country |
| `GET` | `/api/v1/Country/get-delisted-count-by-country?country=` | Number of delisted records for a country |
| `GET` | `/api/v1/Country/get-sanctions-by-country?country=` | Paged records for a country |
| `GET` | `/api/v1/Country/get-delisted-sanctions-by-country?country=` | Paged delisted records for a country |

### Stats

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/Stats/top-countries?top=6` | Countries with the most active records (`top` is clamped to 1–20) |
| `GET` | `/api/v1/Stats/by-sdn-type` | Active-record counts by SDN type |

### Import history

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/logger` | Paged `DataImportLogs` rows. Optional `orderByDescending` (default `true`). |
| `GET` | `/api/v1/logger/error-count` | Number of failed import runs |

---

## Data model

**`SanctionDetails`**

| Column | Notes |
|---|---|
| `Id` | `uniqueidentifier`, primary key |
| `RecordUniqueId` | OFAC `uid`. **Unique index.** |
| `SdnType` | `Individual`, `Entity`, `Vessel` or `Aircraft` |
| `LastName`, `FirstName` | Primary name. **Full-text indexed.** |
| `Country` | Country of the first address, if there is one |
| `XmlRecord` | The original `<sdnEntry>` stored as SQL `xml`, so no detail from the source is lost |
| `IsActive`, `RemovedAtUtc` | Soft-delete fields for delisting |
| `ImportedAtUtc` | When the record was last inserted or changed |

Indexes: `(RecordUniqueId)` unique, `(SdnType, LastName)`, `(IsActive, SdnType, LastName)`, and a full-text index on `(FirstName, LastName)`.

**`DataImportLogs`**: one row per import run, with `RanAtUtc`, `Parsed`, `Inserted`, `Updated`, `Unchanged`, `Removed`, `WasDownloaded`, `Succeeded` and `ErrorMessage`.

---

## Testing and benchmarks

```bash
dotnet test                                                     # all tests
dotnet run -c Release --project DataImport.Benchmarks           # pick a benchmark interactively
dotnet run -c Release --project DataImport.Benchmarks -- --filter '*Xml*'
```

The tests use xUnit and NSubstitute and don't need a database.

---


## Roadmap

- [ ] Index and search aliases (`akaList`)
- [ ] Exclude delisted records from search by default, and let callers set `Tolerance`
- [ ] Authentication (API key or JWT) on all endpoints
- [ ] Integration tests for the upsert and delist logic against a real SQL Server (Testcontainers)
- [ ] Docker Compose setup (SQL Server with full-text search, importer and API)
- [ ] Admin dashboard (React, Vite, TypeScript)
- [ ] More sources: OFAC Consolidated, UN, EU, UK

---

## Contributing

`master` is protected, so changes go through pull requests; direct pushes aren't allowed. CI must pass. Please follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

[MIT](LICENSE.md) © 2026 TheVoid0013

OFAC SDN data is published by the U.S. Department of the Treasury and is in the public domain.
