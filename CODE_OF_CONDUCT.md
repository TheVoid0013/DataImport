# Contributing to DataImport

Thanks for wanting to help! DataImport is a .NET solution that downloads, parses, and upserts OFAC SDN sanctions data into SQL Server, plus a Web API that exposes it. Please read the [Code of Conduct](CODE_OF_CONDUCT.md) before contributing.

## Ways to Contribute

- Report bugs or unexpected import behavior
- Suggest features (new endpoints, better parsing, performance ideas)
- Improve docs or the README
- Fix issues or add tests and benchmarks

For anything big (new project, major refactor, new data source), please **open an issue first** so we can agree on the approach before you invest time.

## Project Layout

| Project | Purpose |
| --- | --- |
| `DataImport` | Console importer: download → parse → save pipeline (MediatR) |
| `DataImport.API` | ASP.NET Core Web API (Swagger/OpenAPI) |
| `DataImport.Data` | Shared data layer |
| `DataImport.Presentation` | Shared DTOs / response shapes |
| `DataImport.Benchmarks` | BenchmarkDotNet comparisons for parsing/save strategies |

## Workflow

1. **Fork** the repo and create a branch from `master`:
   ```bash
   git checkout -b feature/short-description
   ```
   Branch prefixes: `feature/`, `fix/`, `docs/`, `refactor/`, `perf/`.
2. Make your changes in small, focused commits.
3. **Build and test** locally before pushing.
4. Push your branch and **open a pull request** against `master`.

## Coding Guidelines

- Follow the existing **MediatR command/query + handler** pattern; don't bypass it with logic in controllers or `Program.cs`.
- Keep controllers thin. Business logic belongs in handlers.
- Use `async`/`await` with `CancellationToken` where the existing code does.
- Keep memory usage in mind for the importer: stream files, avoid loading the full SDN XML into one string, and keep using batched saves with `ChangeTracker.Clear()`.
- Use `AsNoTracking()` for read-only EF queries.
- Log with **Serilog** using structured logging (`{Property}` templates). No `Console.WriteLine` for diagnostics.
- If you change a model, **add an EF Core migration** and include it in the PR:
  ```bash
  dotnet ef migrations add <DescriptiveName> --project DataImport
  ```
- Follow standard C# conventions: `PascalCase` for types and methods, `_camelCase` for private fields, nullable reference types respected.
- If a change affects performance, include a **BenchmarkDotNet** result (`cd DataImport.Benchmarks && dotnet run -c Release`).

## Security and Data

- **Never commit secrets**: connection strings, API keys, passwords, tokens.
- **Never commit downloaded data.** The `Imports/` cache folder (day-partitioned `sdn.xml` files) should stay out of Git. Make sure it is in `.gitignore`.
- Use public OFAC data or clearly fake data in tests, examples, and bug reports.
- Found a security vulnerability? Please **don't open a public issue**. Contact the maintainer privately instead (see the Code of Conduct for contact details).

## Commit Messages

Short, imperative, descriptive:

```
Add paging to import log endpoint
Fix duplicate upsert when SDN entry is unchanged
Docs: explain cache cleanup behavior
```

## Pull Request Checklist

Please write a **descriptive PR**. In it, include:

- **What** you changed and **why**
- How you **tested** it (steps, sample output, screenshots of Swagger, etc.)
- Linked issue (e.g. `Closes #12`)
- Any **breaking changes** or migration steps

Before submitting, confirm:

- [ ] Builds cleanly with `dotnet build`
- [ ] Existing behavior still works (importer run + API endpoints)
- [ ] EF migration added if the schema changed
- [ ] No secrets or downloaded data committed
- [ ] README/docs updated if behavior changed

## Reporting Bugs

Open an issue with:

- What you expected vs. what happened
- Steps to reproduce
- Relevant logs (with secrets removed)
- Environment: OS, .NET version, SQL Server version

## Questions

Open an issue or reach out to the maintainer, [@TheVoid0013](https://github.com/TheVoid0013). All contributors are expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
