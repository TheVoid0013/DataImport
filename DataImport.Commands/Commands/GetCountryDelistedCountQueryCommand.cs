using DataImport.Commands.Queries;
using DataImport.Data.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.Commands.Commands;

public class GetCountryDelistedCountQueryCommand
    : IRequestHandler<GetCountryDelistedCountQuery, CountryCountResult>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetCountryDelistedCountQueryCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CountryCountResult> Handle(
        GetCountryDelistedCountQuery request,
        CancellationToken ct)
    {
        var country = request.country.ToString();
        var cacheKey = $"CountryDelistedCount_{country}";

        return await _cache.GetOrSetAsync<CountryCountResult>(
            cacheKey,
            async _ =>
            {
                var count = await _db.SanctionDetails
                    .Where(x => x.Country == country && !x.IsActive)
                    .CountAsync(ct);

                return new CountryCountResult(true, country, count, isDelisted: true);
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}