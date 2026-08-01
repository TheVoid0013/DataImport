using DataImport.Commands;
using DataImport.Configuration;
using DataImport.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Configure Serilog before the host builds, so even startup failures get logged.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // EF Core's per-command logging is useful while debugging but way too noisy
    // for a scheduled job's log file — this quiets it to warnings/errors only.
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "Logs", "import-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("=== OFAC SDN import starting ===");

    var builder = Host.CreateApplicationBuilder(args);

    var connectionString = builder.Configuration.GetConnectionString("SanctionsDb");

    builder.Services.AddDbContext<SanctionsDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.CommandTimeout(180);
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        }));

    builder.Services.Configure<ImportSettings>(builder.Configuration.GetSection("ImportSettings"));

    // SDN.xml can run several MB; give it more headroom than the 100s default.
    // treasury.gov also rejects requests with no User-Agent (403), so set one.
    builder.Services.AddHttpClient(nameof(DownloadSdnXmlCommandHandler), client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) DataImport/1.0 (+internal sanctions import tool)");
    });

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(dispose: false); // Log.CloseAndFlushAsync() in finally handles disposal

    using var host = builder.Build();

    var mediator = host.Services.GetRequiredService<IMediator>();

    var result = await mediator.Send(new ImportOfacSdnDataCommand());

    Log.Information(
        "Import complete. Parsed: {Parsed}, Inserted: {Inserted}, Updated: {Updated}, Unchanged: {Unchanged}",
        result.TotalParsed, result.Inserted, result.Updated, result.Unchanged);

    Log.Information("=== OFAC SDN import finished successfully ===");
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "OFAC SDN import failed");
    Environment.ExitCode = 1;
}
finally
{
    // Flushes buffered log events to disk/console before the process exits.
    // Skipping this can silently drop the last few log lines, including the
    // one that would have told you why it failed.
    await Log.CloseAndFlushAsync();
}