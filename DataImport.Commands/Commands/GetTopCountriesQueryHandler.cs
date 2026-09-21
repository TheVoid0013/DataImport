using DataImport.Commands.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.Commands.Commands;

public class GetTopCountriesQueryHandler
    : IRequestHandler<GetTopCountriesQuery, TopCountriesDto>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;

    public GetTopCountriesQueryHandler(SanctionsDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<TopCountriesDto> Handle(
        GetTopCountriesQuery request,
        CancellationToken ct)
    {
        var top = request.Top <= 0 ? 6 : request.Top;

        return await _cache.GetOrSetAsync<TopCountriesDto>(
            $"Stats_TopCountries_{top}",
            async _ =>
            {
                // Only consider active rows
                var baseQuery = _db.SanctionDetails
                    .AsNoTracking()
                    .Where(s => s.IsActive);

                // 1) Total active rows (used for the "Total" figure).
                var total = await baseQuery.CountAsync(ct);

                // 2) Unknown = rows with null/empty country.
                var unknown = await baseQuery
                    .Where(s => s.Country == null || s.Country == "")
                    .CountAsync(ct);

                // 3) Per-country counts for all known countries.
                //    Order on the group (always translatable to SQL),
                //    then project into the DTO as the last step.
                var byCountry = await baseQuery
                    .Where(s => s.Country != null && s.Country != "")
                    .GroupBy(s => s.Country!)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => new CountryCountDto(g.Key, g.Count()))
                    .ToListAsync(ct);

                // 4) Split into top N and "other".
                var topN = byCountry.Take(top).ToList();
                var other = byCountry.Skip(top).Sum(x => x.Count);

                return new TopCountriesDto(
                    Total: total,
                    Unknown: unknown,
                    Other: other,
                    Top: topN
                );
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );
    }
}