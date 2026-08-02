using DataImport.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataImport.Commands
{
    /// <summary>
    /// Gets the raw SDN.XML content as a stream — from today's cache folder if
    /// it's already there, otherwise downloads it from OFAC and caches it.
    ///
    /// Returns a Stream rather than a string so callers (e.g. an XmlReader-based
    /// parser) can process the document without the whole multi-MB file ever
    /// being materialized as one in-memory string. Callers are responsible for
    /// disposing the returned stream.
    /// </summary>
    public record SdnXmlDownloadResult(Stream Content, bool WasDownloaded);
    public record DownloadSdnXmlCommand : IRequest<SdnXmlDownloadResult>;



    public class DownloadSdnXmlCommandHandler : IRequestHandler<DownloadSdnXmlCommand, SdnXmlDownloadResult>
    {
        private const string CachedFileName = "sdn.xml";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ImportSettings _importSettings;
        private readonly ILogger<DownloadSdnXmlCommandHandler> _logger;

        public DownloadSdnXmlCommandHandler(
            IHttpClientFactory httpClientFactory,
            IOptions<ImportSettings> importSettings,
            ILogger<DownloadSdnXmlCommandHandler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _importSettings = importSettings.Value;
            _logger = logger;
        }

        public async Task<SdnXmlDownloadResult> Handle(DownloadSdnXmlCommand request, CancellationToken cancellationToken)
        {
            var todayFolder = GetTodayFolder();
            var cachedFilePath = Path.Combine(todayFolder, CachedFileName);

            bool wasDownloaded;

            if (File.Exists(cachedFilePath))
            {
                _logger.LogInformation("Using cached SDN.XML from {Path}", cachedFilePath);
                wasDownloaded = false;
            }
            else
            {
                _logger.LogInformation("No cache found for today — downloading from {Url}", _importSettings.SdnXmlUrl);
                Directory.CreateDirectory(todayFolder);
                await DownloadToFileAsync(cachedFilePath, cancellationToken);
                _logger.LogInformation("Cached download to {Path}", cachedFilePath);
                wasDownloaded = true;
            }

            // FileOptions.SequentialScan hints to the OS that we'll read this
            // file start-to-end once, which is exactly what an XmlReader does —
            // it enables more aggressive read-ahead caching for this access pattern.
            var stream = new FileStream(
                cachedFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return new SdnXmlDownloadResult(stream, wasDownloaded);
        }

        /// <summary>
        /// Streams the HTTP response body directly to disk. Response headers are
        /// read as soon as they arrive (HttpCompletionOption.ResponseHeadersRead)
        /// so the body itself is never buffered into memory by HttpClient before
        /// we start writing it out — the bytes flow straight from the network
        /// socket to the file.
        /// </summary>
        private async Task DownloadToFileAsync(string destinationPath, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(nameof(DownloadSdnXmlCommandHandler));

            using var response = await client.GetAsync(
                _importSettings.SdnXmlUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to download SDN.XML from {Url}: {StatusCode} {ReasonPhrase}. Response body (first 500 chars): {Body}",
                    _importSettings.SdnXmlUrl, (int)response.StatusCode, response.ReasonPhrase, body[..Math.Min(body.Length, 500)]);

                throw new HttpRequestException(
                    $"Failed to download SDN.XML: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(
                destinationPath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 81920, FileOptions.Asynchronous);

            await httpStream.CopyToAsync(fileStream, cancellationToken);
        }

        private string GetTodayFolder()
        {
            var root = Path.IsPathRooted(_importSettings.RootFolder)
                ? _importSettings.RootFolder
                : Path.Combine(AppContext.BaseDirectory, _importSettings.RootFolder);

            return Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
}