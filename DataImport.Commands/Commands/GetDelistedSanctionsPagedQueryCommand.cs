using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DataImport.Commands.Queries;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.Commands.Commands;

public class GetDelistedSanctionsPagedQueryCommand 
    : IRequestHandler<GetDelistedSanctionsPagedQuery, PagedResult<SanctionListItemDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;
    
    
    public GetDelistedSanctionsPagedQueryCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<SanctionListItemDto>> Handle(
        GetDelistedSanctionsPagedQuery request, CancellationToken ct)
    {
        var cacheKey =
            $"DelistedSanctionsPaged_{request.SdnType}_{request.LastNameContains}_{request.Page}_{request.PageSize}";

        return await _cache.GetOrSetAsync<PagedResult<SanctionListItemDto>>(
            cacheKey,
            async _ =>
            {
                var query = _db.SanctionDetails
                    .AsNoTracking()
                    .Where(s => !s.IsActive); // apply once, before count

                if (!string.IsNullOrWhiteSpace(request.SdnType))
                    query = query.Where(s => s.SdnType == request.SdnType);

                if (!string.IsNullOrWhiteSpace(request.LastNameContains))
                    query = query.Where(s => s.LastName.Contains(request.LastNameContains));

                var total = await query.CountAsync(ct);

                var items = await query
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.Id) // tiebreaker, see below
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => s.ToFacet<SanctionListItemDto>())
                    .ToListAsync(ct);

                return new PagedResult<SanctionListItemDto>(
                    items,
                    request.Page,
                    request.PageSize,
                    total,
                    (int)Math.Ceiling(total / (double)request.PageSize));
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct);
    }
}