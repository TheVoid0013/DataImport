using System.Text;
using System.Xml;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;

namespace DataImport.Benchmarks;

/// <summary>
/// Compares three ways of pulling entity names out of an SDN-shaped XML
/// payload: DOM-style XmlDocument, LINQ-to-XML (XDocument), and a
/// forward-only XmlReader. Useful for deciding whether it's worth switching
/// DownloadSdnXmlCommandHandler's parsing approach as the file grows.
/// </summary>
[MemoryDiagnoser] // reports allocations per run, not just time — usually the more actionable number
public class XmlParsingBenchmarks
{
    private string _xml = string.Empty;

    // Runs once before benchmarks start; builds a synthetic SDN-shaped
    // document so this doesn't depend on a real file being present.
    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();
        sb.Append("<sdnList>");
        for (var i = 0; i < 5000; i++)
        {
            sb.Append($"<sdnEntry><uid>{i}</uid><lastName>Doe{i}</lastName><sdnType>Individual</sdnType></sdnEntry>");
        }
        sb.Append("</sdnList>");
        _xml = sb.ToString();
    }

    [Benchmark(Baseline = true)]
    public int ParseWithXmlDocument()
    {
        var doc = new XmlDocument();
        doc.LoadXml(_xml);
        return doc.GetElementsByTagName("sdnEntry").Count;
    }

    [Benchmark]
    public int ParseWithXDocument()
    {
        var doc = XDocument.Parse(_xml);
        return doc.Descendants("sdnEntry").Count();
    }

    [Benchmark]
    public int ParseWithXmlReader()
    {
        var count = 0;
        using var reader = XmlReader.Create(new StringReader(_xml));
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "sdnEntry")
            {
                count++;
            }
        }
        return count;
    }
}