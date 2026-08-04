using System.Xml.Linq;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    public record LoadAndValidateSdnXmlCommand(Stream RawXml) : IRequest<Stream>;

    public class LoadAndValidateSdnXmlCommandHandler
        : IRequestHandler<LoadAndValidateSdnXmlCommand, Stream>
    {
        private static readonly XNamespace Ns =
            "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/XML";

        // Real file has historically run ~25-30MB. Anything drastically smaller
        // is almost certainly a truncated download, not a real publication.
        private const long MinimumExpectedBytes = 5 * 1024 * 1024; // 5 MB

        // Entry count has hovered around 15k-20k. A number this low signals a
        // corrupted or partial file rather than a real OFAC update.
        private const int MinimumExpectedEntries = 1000;

        private readonly ILogger<LoadAndValidateSdnXmlCommandHandler> _logger;

        public LoadAndValidateSdnXmlCommandHandler(
            ILogger<LoadAndValidateSdnXmlCommandHandler> logger)
        {
            _logger = logger;
        }

        public Task<Stream> Handle(
            LoadAndValidateSdnXmlCommand request,
            CancellationToken cancellationToken)
        {
            if (request.RawXml.CanSeek && request.RawXml.Length < MinimumExpectedBytes)
            {
                throw new SdnFileIntegrityException(
                    $"Downloaded SDN file is only {request.RawXml.Length:N0} bytes " +
                    $"(expected at least {MinimumExpectedBytes:N0}). Likely a truncated or failed download.");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(request.RawXml);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new SdnFileIntegrityException(
                    "Downloaded SDN file is not well-formed XML.", ex);
            }

            if (doc.Root?.Name.Namespace != Ns)
            {
                throw new SdnFileIntegrityException(
                    $"Unexpected root namespace '{doc.Root?.Name.Namespace}'. " +
                    "OFAC may have changed their schema again — check a fresh download manually.");
            }

            var entryCount = doc.Descendants(Ns + "sdnEntry").Count();

            if (entryCount < MinimumExpectedEntries)
            {
                throw new SdnFileIntegrityException(
                    $"Only {entryCount} sdnEntry elements found (expected at least {MinimumExpectedEntries}). " +
                    "File is likely incomplete.");
            }

            _logger.LogInformation(
                "SDN file passed integrity checks: {Entries:N0} entries.",
                entryCount);

            // Rewind the stream so the parser can read it again.
            if (request.RawXml.CanSeek)
            {
                request.RawXml.Position = 0;
            }

            return Task.FromResult(request.RawXml);
        }
    }

    /// <summary>
    /// Thrown when the downloaded SDN file fails a sanity check (too small,
    /// malformed, wrong namespace, or suspiciously few entries).
    /// </summary>
    public class SdnFileIntegrityException : Exception
    {
        public SdnFileIntegrityException(string message) : base(message) { }

        public SdnFileIntegrityException(string message, Exception inner)
            : base(message, inner) { }
    }
}