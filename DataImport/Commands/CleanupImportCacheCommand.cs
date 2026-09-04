using DataImport.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using MethodTimer;

namespace DataImport.Commands
{
    /// <summary>
    /// Deletes cached sdn.xml date-folders (yyyy-MM-dd, created by
    /// DownloadSdnXmlCommandHandler) older than ImportSettings.CacheRetentionDays.
    /// Runs at the start of each import so the cache folder doesn't grow forever.
    /// </summary>
    public record CleanupImportCacheCommand : IRequest<int>;

    public class CleanupImportCacheCommandHandler : IRequestHandler<CleanupImportCacheCommand, int>
    {
        private readonly ImportSettings _importSettings;
        private readonly ILogger<CleanupImportCacheCommandHandler> _logger;

        public CleanupImportCacheCommandHandler(
            IOptions<ImportSettings> importSettings,
            ILogger<CleanupImportCacheCommandHandler> logger)
        {
            _importSettings = importSettings.Value;
            _logger = logger;
        }

        [Time]
        public Task<int> Handle(CleanupImportCacheCommand request, CancellationToken cancellationToken)
        {
            if (_importSettings.CacheRetentionDays <= 0)
            {
                _logger.LogInformation("Cache retention disabled (CacheRetentionDays <= 0) — skipping cleanup.");
                return Task.FromResult(0);
            }

            var root = Path.IsPathRooted(_importSettings.RootFolder)
                ? _importSettings.RootFolder
                : Path.Combine(AppContext.BaseDirectory, _importSettings.RootFolder);

            if (!Directory.Exists(root))
            {
                // Nothing cached yet — nothing to clean.
                return Task.FromResult(0);
            }

            var cutoff = DateTime.Now.Date.AddDays(-_importSettings.CacheRetentionDays);
            var deleted = 0;

            foreach (var folder in Directory.GetDirectories(root))
            {
                var folderName = Path.GetFileName(folder);

                // Only touch folders that match our own yyyy-MM-dd naming convention —
                // anything else in RootFolder wasn't created by us, so leave it alone.
                if (!DateTime.TryParseExact(
                        folderName, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var folderDate))
                {
                    continue;
                }

                if (folderDate >= cutoff)
                {
                    continue;
                }

                try
                {
                    Directory.Delete(folder, recursive: true);
                    deleted++;
                    _logger.LogInformation("Deleted expired cache folder {Folder}", folder);
                }
                catch (Exception ex)
                {
                    // A locked/in-use folder shouldn't fail the whole import —
                    // just log it and move on; it'll be retried next run.
                    _logger.LogWarning(ex, "Failed to delete expired cache folder {Folder}", folder);
                }
            }

            if (deleted > 0)
            {
                _logger.LogInformation("Cache cleanup removed {Count} expired folder(s).", deleted);
            }

            return Task.FromResult(deleted);
        }
    }
}