using System.Xml.Linq;
using DataImport.Data.Models;
using MediatR;
using MethodTimer;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    /// <summary>
    /// Parses raw SDN.XML into individual SanctionDetail records — one per &lt;sdnEntry&gt;.
    /// Fails loudly on malformed input: a silently skipped entry would later look
    /// like a delisting to the removal sweep.
    /// </summary>
    public record ParseSdnXmlCommand(Stream RawXml) : IRequest<List<SanctionDetail>>;


    public class ParseSdnXmlCommandHandler : IRequestHandler<ParseSdnXmlCommand, List<SanctionDetail>>
    {
        // OFAC changed this namespace on 05/07/2024. If parsing suddenly returns
        // zero records, this is the first thing to check — inspect the root
        // <sdnList> element's xmlns attribute in a fresh download.
        private static readonly XNamespace Ns =
            "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/XML";

        private readonly ILogger<ParseSdnXmlCommandHandler> _logger;

        public ParseSdnXmlCommandHandler(ILogger<ParseSdnXmlCommandHandler> logger)
        {
            _logger = logger;
        }

        [Time]
        public Task<List<SanctionDetail>> Handle(ParseSdnXmlCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parsing SDN XML ({Length} bytes)...", request.RawXml.Length);

            var doc = XDocument.Load(request.RawXml);

            var results = new List<SanctionDetail>();
            var seenUids = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in doc.Descendants(Ns + "sdnEntry"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var uid = entry.Element(Ns + "uid")?.Value;
                if (string.IsNullOrWhiteSpace(uid))
                {
                    throw new InvalidOperationException(
                        "Encountered an sdnEntry with no uid; refusing to import a partial list.");
                }

                if (!seenUids.Add(uid))
                {
                    throw new InvalidOperationException(
                        $"Duplicate sdnEntry uid {uid} in file; refusing to import.");
                }

                var sdnType = entry.Element(Ns + "sdnType")?.Value;
                var lastName = entry.Element(Ns + "lastName")?.Value;
                var firstName = entry.Element(Ns + "firstName")?.Value;

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(sdnType))
                {
                    // Both are required by OFAC's schema. Skipping would make this entry
                    // look delisted to the removal sweep, so fail the whole import instead.
                    throw new InvalidOperationException(
                        $"sdnEntry {uid} is missing required lastName or sdnType; refusing to import a partial list.");
                }

                var country = entry.Element(Ns + "addressList")?
                                    .Element(Ns + "address")?
                                    .Element(Ns + "country")?
                                    .Value;

                results.Add(new SanctionDetail
                {
                    RecordUniqueId = uid,
                    SdnType = sdnType,
                    LastName = lastName,
                    FirstName = firstName,
                    Country = country,
                    XmlRecord = entry.ToString(SaveOptions.DisableFormatting)
                });
            }

            if (results.Count == 0)
            {
                throw new InvalidOperationException(
                    "Parsed 0 sdnEntry records. Check the XML namespace (Ns) against the root <sdnList> xmlns of a fresh download.");
            }

            _logger.LogInformation("Parsed {Count} SDN entries.", results.Count);

            return Task.FromResult(results);
        }
    }
}