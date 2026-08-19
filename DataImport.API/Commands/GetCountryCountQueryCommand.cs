using DataImport.API.Queries;
using DataImport.Data.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.API.Commands;

public class GetCountryCountQueryCommand : IRequestHandler<GetCountryCountQuery, object>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetCountryCountQueryCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<object> Handle(GetCountryCountQuery request, CancellationToken ct)
    {
        var cacheKey = $"CountryCount_{request.country}";

        return await _cache.GetOrSetAsync<object>(
            cacheKey,
            async _ =>
            {
                var count = await _db.SanctionDetails
                    .Where(x => x.Country == request.country.ToString())
                    .CountAsync(ct);

                return new
                {
                    Success = true,
                    Countrry = request.country,
                    Count = count
                };
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}