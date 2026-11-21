using DataImport.Commands;
using DataImport.Extensions;
using DataImport.Logging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DataImport.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DataImport.Hosting;

/// <summary>
/// Top-level orchestration for the OFAC SDN import job: configures logging,
/// builds the host, runs the import, and guarantees logs are flushed on the
/// way out. This is the only thing Program.cs needs to call.
/// </summary>
internal static class ImportRunner
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(15);

    public static async Task<int> RunAsync(string[] args)
    {
        SerilogBootstrapper.Initialize();

        try
        {
            Log.Information("=== OFAC SDN import starting ===");

            using var host = BuildHost(args);
            MethodTimeLogger.Configure(host.Services.GetRequiredService<ILoggerFactory>());
            using var scope = host.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await RunWithRetryAsync(mediator);

            Log.Information(
                "Import complete. Parsed: {Parsed}, Inserted: {Inserted}, Updated: {Updated}, Unchanged: {Unchanged}",
                result.TotalParsed, result.Inserted, result.Updated, result.Unchanged);

            Log.Information("=== OFAC SDN import finished successfully ===");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "OFAC SDN import failed after {MaxAttempts} attempt(s)", MaxAttempts);
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    internal static async Task<ImportOfacSdnDataResult> RunWithRetryAsync(
        IMediator mediator,
        int maxAttempts = MaxAttempts,
        TimeSpan? retryDelay = null)
    {
        var delay = retryDelay ?? RetryDelay;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await mediator.Send(new ImportOfacSdnDataCommand());
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Log.Warning(ex,
                    "Import attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay}...",
                    attempt, maxAttempts, delay);

                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }

    private static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder
            .AddSanctionsDbContext()
            .AddImportSettings()
            .AddSdnDownloadHttpClient()
            .AddImportMediatR()
            .AddEmailNotifications()
            .UseSerilogLogging();

        return builder.Build();
    }
}