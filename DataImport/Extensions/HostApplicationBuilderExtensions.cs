using DataImport.Commands;
using DataImport.Configuration;
using DataImport.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DataImport.Extensions;

/// <summary>
/// Registration for each service the import job depends on. Split into small,
/// single-purpose methods so Program.cs reads as a checklist rather than a
/// wall of configuration.
/// </summary>
internal static class HostApplicationBuilderExtensions
{
    public static HostApplicationBuilder AddSanctionsDbContext(this HostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("SanctionsDb");

        builder.Services.AddDbContext<SanctionsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(180);
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            }));

        return builder;
    }

    public static HostApplicationBuilder AddImportSettings(this HostApplicationBuilder builder)
    {
        builder.Services.Configure<ImportSettings>(builder.Configuration.GetSection("ImportSettings"));
        return builder;
    }

    public static HostApplicationBuilder AddSdnDownloadHttpClient(this HostApplicationBuilder builder)
    {
        // SDN.xml can run several MB; give it more headroom than the 100s default.
        // treasury.gov also rejects requests with no User-Agent (403), so set one.
        builder.Services.AddHttpClient(nameof(DownloadSdnXmlCommandHandler), client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) DataImport/1.0 (+internal sanctions import tool)");
        });

        return builder;
    }

    public static HostApplicationBuilder AddImportMediatR(this HostApplicationBuilder builder)
    {
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(HostApplicationBuilderExtensions).Assembly));

        return builder;
    }

    public static HostApplicationBuilder UseSerilogLogging(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: false); // Log.CloseAndFlushAsync() in finally handles disposal
        return builder;
    }
}