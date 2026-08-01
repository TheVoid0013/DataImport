using System.Xml.Linq;
using DataImport.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    /// <summary>
    /// Parses raw SDN.XML into individual SanctionDetail records — one per &lt;sdnEntry&gt;.
    /// </summary>
    public record ParseSdnXmlCommand(string RawXml) : IRequest<List<SanctionDetail>>;

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

        public Task<List<SanctionDetail>> Handle(ParseSdnXmlCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parsing SDN XML ({Length} bytes)...", request.RawXml.Length);

            var doc = XDocument.Parse(request.RawXml);

            var results = new List<SanctionDetail>();

            foreach (var entry in doc.Descendants(Ns + "sdnEntry"))
            {
                var uid = entry.Element(Ns + "uid")?.Value;
                if (string.IsNullOrWhiteSpace(uid))
                {
                    // Shouldn't happen in practice, but skip rather than blow up the whole import.
                    continue;
                }

                var country = entry.Element(Ns + "addressList")?
                                    .Element(Ns + "address")?
                                    .Element(Ns + "country")?
                                    .Value;

                results.Add(new SanctionDetail
                {
                    RecordUniqueId = uid,
                    Country = country,
                    XmlRecord = entry.ToString(SaveOptions.DisableFormatting)
                });
            }

            _logger.LogInformation("Parsed {Count} SDN entries.", results.Count);

            return Task.FromResult(results);
        }
    }
}