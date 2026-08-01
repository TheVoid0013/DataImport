using DataImport.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataImport.Commands
{
    /// <summary>
    /// Gets the raw SDN.XML content — from today's cache folder if it's
    /// already there, otherwise downloads it from OFAC and caches it.
    /// </summary>
    public record DownloadSdnXmlCommand : IRequest<string>;

    public class DownloadSdnXmlCommandHandler : IRequestHandler<DownloadSdnXmlCommand, string>
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

        public async Task<string> Handle(DownloadSdnXmlCommand request, CancellationToken cancellationToken)
        {
            var todayFolder = GetTodayFolder();
            var cachedFilePath = Path.Combine(todayFolder, CachedFileName);

            if (File.Exists(cachedFilePath))
            {
                _logger.LogInformation("Using cached SDN.XML from {Path}", cachedFilePath);
                return await File.ReadAllTextAsync(cachedFilePath, cancellationToken);
            }

            _logger.LogInformation("No cache found for today — downloading from {Url}", _importSettings.SdnXmlUrl);
            var xml = await DownloadAsync(cancellationToken);

            Directory.CreateDirectory(todayFolder);
            await File.WriteAllTextAsync(cachedFilePath, xml, cancellationToken);
            _logger.LogInformation("Cached download to {Path}", cachedFilePath);

            return xml;
        }

        private async Task<string> DownloadAsync(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(nameof(DownloadSdnXmlCommandHandler));

            using var response = await client.GetAsync(_importSettings.SdnXmlUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to download SDN.XML from {Url}: {StatusCode} {ReasonPhrase}. Response body (first 500 chars): {Body}",
                    _importSettings.SdnXmlUrl, (int)response.StatusCode, response.ReasonPhrase, body[..Math.Min(body.Length, 500)]);

                throw new HttpRequestException(
                    $"Failed to download SDN.XML: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
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