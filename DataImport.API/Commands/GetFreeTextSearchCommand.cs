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
    : IRequestHandler<GetFreeTextSearchQuery, List<FreeTextSearchResultDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetFreeTextSearchQueryHandler(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<FreeTextSearchResultDto>> Handle(
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
            return new List<FreeTextSearchResultDto>();

        var cacheKey = $"FreeTextSearch_OR_{string.Join("_", parts)}";

        var results = await _cache.GetOrSetAsync<List<FreeTextSearchResultDto>>(
            cacheKey,
            async _ =>
            {
                // Build: word1 in (First OR Last) OR word2 in (First OR Last) OR ...
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

                return matches.Select(r => r.ToFacet<FreeTextSearchResultDto>()).ToList();
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );

        return results;
    }
}