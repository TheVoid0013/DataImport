using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DataImport.API.Queries;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.API.Commands;

public class GetCountrySanctionsPagedQueryHandler
    : IRequestHandler<GetCountrySanctionsPagedQuery, PagedResult<SanctionListItemDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetCountrySanctionsPagedQueryHandler(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<SanctionListItemDto>> Handle(
        GetCountrySanctionsPagedQuery request, CancellationToken ct)
    {
        var countryName = request.country.ToString();
        var cacheKey = $"CountrySanctionsPaged_{countryName}_{request.Page}_{request.PageSize}";

        return await _cache.GetOrSetAsync<PagedResult<SanctionListItemDto>>(
            cacheKey,
            async _ =>
            {
                var query = _db.SanctionDetails
                    .AsNoTracking()
                    .Where(x => x.Country == countryName);

                var total = await query.CountAsync(ct);

                var items = await query
                    .OrderBy(x => x.Id)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => x.ToFacet<SanctionListItemDto>())
                    .ToListAsync(ct);

                return new PagedResult<SanctionListItemDto>(
                    items,
                    request.Page,
                    request.PageSize,
                    total,
                    (int)Math.Ceiling(total / (double)request.PageSize)
                );
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}