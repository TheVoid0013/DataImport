using DataImport.API.Queries;
using DataImport.API.Services;
using DataImport.Data.Data;
using DataImport.Data.Models;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;
using LinqKit;

namespace DataImport.API.Commands;

public class GetFreeTextSearchQueryHandler
    : IRequestHandler<GetFreeTextSearchQuery, FreeTextSearchResponseDto>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _cache;
    private readonly IMediator _mediator;
    private readonly INameSimilarityScorer _scorer;

    public GetFreeTextSearchQueryHandler(
        SanctionsDbContext db, IFusionCache cache, IMediator mediator, INameSimilarityScorer scorer)
    {
        _db = db;
        _cache = cache;
        _mediator = mediator;
        _scorer = scorer;
    }

    public async Task<FreeTextSearchResponseDto> Handle(
        GetFreeTextSearchQuery request,
        CancellationToken ct)
    {
        // Seperate a Name into Multiple parts
        // Also removes Noisy words to create a Better Result.
        var parts = await _mediator.Send(new SanitizeSearchTermsQuery(request.Name), ct);
        if (parts.Length == 0)
            return new FreeTextSearchResponseDto();

        // Cache key is keyed on `parts` only — the DB round-trip is what's expensive
        // and reusable across callers. Scoring/tolerance is cheap in-memory work
        // and depends on the raw request.Name + request.Tolerance, so it stays
        // outside the cached delegate — otherwise the first caller's Tolerance
        // would silently apply to every caller for the next 5 minutes.
        var cacheKey = $"FreeTextSearch_OR_{string.Join("_", parts)}";
        var matches = await _cache.GetOrSetAsync<List<SanctionDetail>>(
            cacheKey,
            async _ =>
            {
                var predicate = PredicateBuilder.New<SanctionDetail>(false);
                foreach (var word in parts)
                {
                    var w = word;
                    predicate = predicate.Or(s =>
                        EF.Functions.FreeText(s.FirstName!, w) ||
                        EF.Functions.FreeText(s.LastName, w));
                }

                return await _db.SanctionDetails
                    .AsNoTracking()
                    .Where(predicate)
                    .ToListAsync(ct);
            },
            options => options.SetDuration(TimeSpan.FromMinutes(5)),
            ct
        );

        // Score every candidate against the original (unsanitized) name and
        // keep only what clears the caller's tolerance threshold.
        var scored = matches
            .Select(m => new
            {
                Model = m,
                Score = _scorer.Score(request.Name, $"{m.FirstName} {m.LastName}".Trim()) * 100
            })
            .Where(x => x.Score >= request.Tolerance)
            .OrderByDescending(x => x.Score)
            .ToList();

        var resultDtos = scored.Select(x =>
        {
            var dto = x.Model.ToFacet<FreeTextSearchResultDto>();
            dto.MatchScore = Math.Round(x.Score, 2);
            return dto;
        }).ToList();

        return new FreeTextSearchResponseDto
        {
            TotalCount = resultDtos.Count,
            DistinctSdnTypes = scored
                .Select(x => x.Model.SdnType)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList(),

            DistinctCountries = scored
                .Select(x => x.Model.Country)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList(),

            Results = resultDtos
        };
    }
}