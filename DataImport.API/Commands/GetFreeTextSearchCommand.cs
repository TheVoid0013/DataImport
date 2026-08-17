using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

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
        var cacheKey = $"FreeTextSearch_{request.Name.Trim().ToLowerInvariant()}";

        var results = await _cache.GetOrSetAsync<List<FreeTextSearchResultDto>>(
            cacheKey,
            async _ =>
            {
                var parts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var query = _db.SanctionDetails.AsNoTracking().AsQueryable();

                if (parts.Length > 1)
                {
                    var first = parts[0];
                    var last = parts[1];

                    query = query.Where(s =>
                        (EF.Functions.FreeText(s.FirstName!, first) && EF.Functions.FreeText(s.LastName, last))
                        || (EF.Functions.FreeText(s.FirstName!, last) && EF.Functions.FreeText(s.LastName, first)));
                }
                else
                {
                    var term = request.Name;

                    query = query.Where(s =>
                        EF.Functions.FreeText(s.FirstName!, term)
                        || EF.Functions.FreeText(s.LastName, term));
                }

                var matches = await query.ToListAsync(ct);
                return matches.Select(r => r.ToFacet<FreeTextSearchResultDto>()).ToList();
            },
            options =>
            {
                options.SetDuration(TimeSpan.FromMinutes(5));
            },
            ct
        );

        return results;
    }
}