using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DataImport.Diagnostics;

public static class MethodTimeLogger
{
    private static ILoggerFactory? _loggerFactory;

    public static void Configure(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory;

    public static void Log(MethodBase methodBase, TimeSpan elapsed, string? message)
    {
        try
        {
            var categoryName = methodBase.DeclaringType?.FullName ?? "MethodTimer";
            var logger = _loggerFactory?.CreateLogger(categoryName);

            logger?.LogInformation(
                "{Method} took {ElapsedMs}ms {Message}",
                methodBase.Name,
                elapsed.TotalMilliseconds,
                message);
        }
        catch (ObjectDisposedException)
        {
            // Host already tore down before a late async timer continuation fired
            // safe to ignore.
        }
    }
}