using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;

namespace DataImport.Benchmarks;

/// <summary>
/// Compares the OLD approach (read whole file into a string, then hand that
/// string to a parser) against the NEW approach (open a FileStream, feed it
/// straight into XmlReader) — mirroring the before/after of the
/// DownloadSdnXmlCommandHandler refactor.
///
/// To benchmark against a real cached sdn.xml instead of the generated
/// stand-in, set RealFilePath below to that file's path.
/// </summary>
[MemoryDiagnoser]
public class SdnHandlerApproachBenchmarks
{
    // Set this to a real cached sdn.xml path to benchmark against actual data,
    // e.g. @"D:\DataImport\Cache\2026-08-01\sdn.xml". Leave null to use the
    // generated 50k-entry stand-in file instead.
    private static readonly string? RealFilePath = null;

    private string _filePath = string.Empty;
    private const int GeneratedEntryCount = 50_000;

    [GlobalSetup]
    public void Setup()
    {
        if (RealFilePath is not null)
        {
            _filePath = RealFilePath;
            return;
        }

        _filePath = Path.Combine(Path.GetTempPath(), "sdn-benchmark-sample.xml");

        if (File.Exists(_filePath))
        {
            return; // reuse across runs instead of regenerating a large file every time
        }

        using var writer = new StreamWriter(_filePath, append: false, Encoding.UTF8);
        writer.Write("<sdnList>");
        for (var i = 0; i < GeneratedEntryCount; i++)
        {
            writer.Write($"<sdnEntry><uid>{i}</uid><lastName>Doe{i}</lastName><sdnType>Individual</sdnType></sdnEntry>");
        }
        writer.Write("</sdnList>");
    }

    /// <summary>
    /// OLD behavior: File.ReadAllTextAsync loads the whole file into a string
    /// (like the original handler did), then the string is parsed.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> OldApproach_ReadAllTextThenParse()
    {
        var xml = await File.ReadAllTextAsync(_filePath);

        var count = 0;
        using var reader = XmlReader.Create(new StringReader(xml));
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "sdnEntry")
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// NEW behavior: open a FileStream (like the refactored handler returns)
    /// and feed it straight into XmlReader — no intermediate string.
    /// </summary>
    [Benchmark]
    public int NewApproach_StreamDirectlyToXmlReader()
    {
        using var stream = new FileStream(
            _filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, FileOptions.SequentialScan);

        var count = 0;
        using var reader = XmlReader.Create(stream);
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