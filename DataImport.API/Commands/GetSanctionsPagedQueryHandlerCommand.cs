using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DataImport.API.Queries;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.API.Commands;

public class GetSanctionsPagedQueryHandlerCommand : IRequestHandler<GetSanctionsPagedQuery, PagedResult<SanctionListItemDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetSanctionsPagedQueryHandlerCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }
    
    // Pseudo plan:
    // Firstly, create a cache key 
    // first search in cache then only query
    // return the value
    // cache the value
    

    public async Task<PagedResult<SanctionListItemDto>> Handle(GetSanctionsPagedQuery request, CancellationToken ct)
    {
        // Generate a unique cache key based on request parameters
        var cacheKey = $"SanctionsPaged_{request.SdnType}_{request.LastNameContains}_{request.Page}_{request.PageSize}";


        var cachedResult = await _cache.GetOrSetAsync<PagedResult<SanctionListItemDto>>(
            cacheKey,
            async _ =>
            {
                var query = _db.SanctionDetails.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SdnType))
                    query = query.Where(s => s.SdnType == request.SdnType);

                if (!string.IsNullOrWhiteSpace(request.LastNameContains))
                    query = query.Where(s => s.LastName.Contains(request.LastNameContains));

                var total = await query.CountAsync(ct);

                var items = await query
                    .OrderBy(s => s.LastName)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => s.ToFacet<SanctionListItemDto>())
                    .ToListAsync(ct);

                return new PagedResult<SanctionListItemDto>(
                    items,
                    request.Page,
                    request.PageSize,
                    total,
                    (int)Math.Ceiling(total / (double)request.PageSize)
                );
            },
            options =>
            {
                options.SetDuration(TimeSpan.FromMinutes(5));
            },
            ct
        );

        return cachedResult;
    }
}