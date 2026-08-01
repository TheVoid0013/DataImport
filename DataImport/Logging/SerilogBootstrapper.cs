using Serilog;
using Serilog.Events;

namespace DataImport.Logging;

/// <summary>
/// Configures the static Serilog logger used before and during host startup.
/// Kept separate from Program.cs so the "pre-host" logging concern (which
/// exists specifically to catch startup failures the host itself can't log)
/// doesn't get lost in the middle of composition-root code.
/// </summary>
internal static class SerilogBootstrapper
{
    private const string LogFileNameTemplate = "import-.log";

    /// <summary>
    /// Builds and assigns the global Serilog.Log.Logger. Must be called before
    /// the host is built so that host-build failures are still logged.
    /// </summary>
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            // EF Core's per-command logging is useful while debugging but way too
            // noisy for a scheduled job's log file — quiet it to warnings/errors only.
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "Logs", LogFileNameTemplate),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}