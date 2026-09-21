using DataImport.Commands.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.Commands.Commands;

public class GetSdnTypeCountsQueryHandler
    : IRequestHandler<GetSdnTypeCountsQuery, List<SdnTypeCountDto>>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetSdnTypeCountsQueryHandler(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<SdnTypeCountDto>> Handle(
        GetSdnTypeCountsQuery request,
        CancellationToken ct)
    {
        return await _cache.GetOrSetAsync<List<SdnTypeCountDto>>(
            "Stats_SdnTypeCounts",
            async _ =>
            {
                // Only consider active rows.
                var baseQuery = _db.SanctionDetails
                    .AsNoTracking()
                    .Where(s => s.IsActive);

                // Order on the group (translates cleanly to SQL),
                // then project into the DTO as the last step.
                return await baseQuery
                    .GroupBy(s => s.SdnType)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => new SdnTypeCountDto(g.Key, g.Count()))
                    .ToListAsync(ct);
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}