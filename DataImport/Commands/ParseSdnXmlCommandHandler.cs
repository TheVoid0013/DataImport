using System.Xml.Linq;
using DataImport.Data.Models;
using MediatR;
using MethodTimer;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    /// <summary>
    /// Parses raw SDN.XML into individual SanctionDetail records — one per &lt;sdnEntry&gt;.
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

                var sdnType = entry.Element(Ns + "sdnType")?.Value;
                var lastName = entry.Element(Ns + "lastName")?.Value;
                var firstName = entry.Element(Ns + "firstName")?.Value; 

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(sdnType))
                {
                    // Both are required by OFAC's schema. If either is missing, this entry
                    // is malformed — skip it rather than let a NOT NULL violation blow up
                    // the whole batch save later.
                    _logger.LogWarning("Skipping sdnEntry {Uid} — missing required lastName or sdnType.", uid);
                    continue;
                }

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

            _logger.LogInformation("Parsed {Count} SDN entries.", results.Count);

            return Task.FromResult(results);
        }
    }
}