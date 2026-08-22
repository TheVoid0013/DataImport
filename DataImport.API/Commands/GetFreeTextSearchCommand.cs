using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Data.Models;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;
using LinqKit;

namespace DataImport.API.Commands;


public class GetFreeTextSearchQueryHandler
    : IRequestHandler<GetFreeTextSearchQuery, FreeTextSearchResponseDto>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetFreeTextSearchQueryHandler(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<FreeTextSearchResponseDto> Handle(
        GetFreeTextSearchQuery request,
        CancellationToken ct)
    {
        var parts = request.Name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct()
            .OrderBy(p => p)
            .ToArray();

        if (parts.Length == 0)
            return new FreeTextSearchResponseDto();

        var cacheKey = $"FreeTextSearch_OR_{string.Join("_", parts)}";

        var response = await _cache.GetOrSetAsync<FreeTextSearchResponseDto>(
            cacheKey,
            async _ =>
            {
                var predicate = PredicateBuilder.New<SanctionDetail>(false);
                foreach (var word in parts)
                {
                    var w = word;
                    predicate = predicate.Or(s =>
                        EF.Functions.FreeText(s.FirstName!, w) ||
                        EF.Functions.FreeText(s.LastName, w));
                }

                var matches = await _db.SanctionDetails
                    .AsNoTracking()
                    .Where(predicate)
                    .ToListAsync(ct);

                return new FreeTextSearchResponseDto
                {
                    TotalCount = matches.Count,
                    DistinctSdnTypes = matches
                        .Select(m => m.SdnType.ToString())   // drop .ToString() if SdnType is already string
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList(),
                    DistinctCountries = matches
                        .Select(m => m.Country)              // drop .ToString() here too if already string
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList(),
                    Results = matches.Select(r => r.ToFacet<FreeTextSearchResultDto>()).ToList()
                };
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );

        return response;
    }
}
