using System.Text.RegularExpressions;
using MediatR;
using DataImport.API.Queries;

namespace DataImport.API.Commands.Helper;

public class SanitizeSearchTermsQueryCommand : IRequestHandler<SanitizeSearchTermsQuery, string[]>
{
    // Matches any character that is NOT a letter, digit, or whitespace.
    private static readonly Regex NonAlphaNumeric = new(
        @"[^\w\s]|_",
        RegexOptions.Compiled);

    private static readonly HashSet<string> LegalSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "limited", "ltd", "inc", "incorporated", "llc", "co", "corp",
        "corporation", "company", "plc", "group", "holdings", "holding",
        "sa", "gmbh", "srl", "bv", "nv", "pty", "ag", "sarl", "spa"
    };

    private static readonly HashSet<string> ConditionalNoisyWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bank", "trust", "trading", "insurance", "services", "enterprise",
        "enterprises", "industries", "international", "global", "capital",
        "investments", "ventures"
    };

    private static readonly HashSet<string> PersonalTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mrs", "ms", "miss", "dr", "prof", "sir", "madam",
        "king", "queen", "prince", "princess", "sheikh", "sheikha",
        "hajji", "hon", "rev"
    };

    // New: strip all punctuation/symbols, collapse to single spaces, before tokenizing.
    private static string CleanSymbols(string input)
    {
        var replaced = NonAlphaNumeric.Replace(input, " ");
        return Regex.Replace(replaced, @"\s+", " ").Trim();
    }

    public Task<string[]> Handle(SanitizeSearchTermsQuery request, CancellationToken ct)
    {
        var cleanedInput = CleanSymbols(request.Name);

        var rawParts = cleanedInput
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (rawParts.Length == 0)
            return Task.FromResult(Array.Empty<string>());

        var afterHardStrip = rawParts
            .Where(p => !LegalSuffixes.Contains(p) && !PersonalTitles.Contains(p))
            .ToArray();

        var baseParts = afterHardStrip.Length > 0 ? afterHardStrip : rawParts;

        var afterConditionalStrip = baseParts
            .Where(p => !ConditionalNoisyWords.Contains(p))
            .ToArray();

        var parts = (afterConditionalStrip.Length > 0 ? afterConditionalStrip : baseParts)
            .OrderBy(p => p)
            .ToArray();

        return Task.FromResult(parts);
    }
}