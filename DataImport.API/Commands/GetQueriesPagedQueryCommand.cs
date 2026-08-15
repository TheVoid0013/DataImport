using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Data.Models;
using DataImport.Presentation.GenericDTO;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Facet.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.API.Commands;

public class GetQueriesPagedQueryCommand : IRequestHandler<GetQueriesPagedQuery, PagedResult<ImportLogDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetQueriesPagedQueryCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<ImportLogDto>> Handle(GetQueriesPagedQuery request, CancellationToken ct)
    {
        
        var cacheKey= $"ImportLogs_{request.Page}_{request.PageSize}";
        var cacheKeyResult = await _cache.GetOrSetAsync<PagedResult<ImportLogDto>>(
            cacheKey,
            async _ =>
            {
                var query = _db.DataImportLogs.AsNoTracking().AsQueryable();

                var total = await query.CountAsync(ct);

                query = request.OrderByDescending
                    ? query.OrderByDescending(x => x.RanAtUtc)
                    : query.OrderBy(x => x.RanAtUtc);

                var items = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => s.ToFacet<ImportLogDto>())
                    .ToListAsync(ct);

                return new PagedResult<ImportLogDto>(
                    Items: items,
                    Page: request.Page,
                    PageSize: request.PageSize,
                    TotalCount: total,
                    TotalPages: (int)Math.Ceiling(total / (double)request.PageSize)
                );
            },
            options =>
            {
                options.SetDuration(TimeSpan.FromMinutes(5));
            },
            ct
        );
        
        return cacheKeyResult;

    }
}