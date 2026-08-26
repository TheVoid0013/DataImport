using DataImport.Commands.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore; 
using ZiggyCreatures.Caching.Fusion;

namespace DataImport.API.Commands;

public class GetSanctionByIdQueryHandlerCommand
    : IRequestHandler<GetSanctionByIdQuery, SanctionDetailDto?>
{
    private readonly SanctionsDbContext _db;
    private readonly IFusionCache _fusionCache;

    public GetSanctionByIdQueryHandlerCommand(SanctionsDbContext db, IFusionCache fusionCache)
    {
        _db = db;
        _fusionCache = fusionCache;
    }

    public async Task<SanctionDetailDto?> Handle(
        GetSanctionByIdQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"Sanctions{request.Id}";

        var cacheResult = await _fusionCache.GetOrSetAsync<SanctionDetailDto>(
            cacheKey,
            async _ =>
            {
                var entity = await _db.SanctionDetails
                    .FirstOrDefaultAsync(x => x.RecordUniqueId == request.Id, ct);

                return entity?.ToFacet<SanctionDetailDto>();
            },
            options =>
            {
                options.SetDuration(TimeSpan.FromMinutes(67));
            },
            ct);

        return cacheResult;
    }
}