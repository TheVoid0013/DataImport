using DataImport.Commands.Queries;
using DataImport.Data.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.Commands.Commands;

public record CountryCountResult(bool Success, String Country, int Count);

public class GetCountryCountQueryCommand : IRequestHandler<GetCountryCountQuery, CountryCountResult>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetCountryCountQueryCommand(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CountryCountResult> Handle(GetCountryCountQuery request, CancellationToken ct)
    {
        var cacheKey = $"CountryCount_{request.country}";

        return await _cache.GetOrSetAsync<CountryCountResult>(
            cacheKey,
            async _ =>
            {
                var count = await _db.SanctionDetails
                    .Where(x => x.Country == request.country.ToString())
                    .CountAsync(ct);

                return new CountryCountResult(true, request.country.ToString(), count);
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}