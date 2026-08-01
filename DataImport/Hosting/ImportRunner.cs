using DataImport.Commands;
using DataImport.Extensions;
using DataImport.Logging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DataImport.Hosting;

/// <summary>
/// Top-level orchestration for the OFAC SDN import job: configures logging,
/// builds the host, runs the import, and guarantees logs are flushed on the
/// way out. This is the only thing Program.cs needs to call.
/// </summary>
internal static class ImportRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        // Configure Serilog before the host builds, so even startup failures get logged.
        SerilogBootstrapper.Initialize();

        try
        {
            Log.Information("=== OFAC SDN import starting ===");

            using var host = BuildHost(args);

            var mediator = host.Services.GetRequiredService<IMediator>();
            var result = await mediator.Send(new ImportOfacSdnDataCommand());

            Log.Information(
                "Import complete. Parsed: {Parsed}, Inserted: {Inserted}, Updated: {Updated}, Unchanged: {Unchanged}",
                result.TotalParsed, result.Inserted, result.Updated, result.Unchanged);

            Log.Information("=== OFAC SDN import finished successfully ===");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "OFAC SDN import failed");
            return 1;
        }
        finally
        {
            // Flushes buffered log events to disk/console before the process exits.
            // Skipping this can silently drop the last few log lines, including the
            // one that would have told you why it failed.
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder
            .AddSanctionsDbContext()
            .AddImportSettings()
            .AddSdnDownloadHttpClient()
            .AddImportMediatR()
            .UseSerilogLogging();

        return builder.Build();
    }
}