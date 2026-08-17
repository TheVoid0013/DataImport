using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DataImport.API.Commands;

public class GetFreeTextSearchQueryHandler
    : IRequestHandler<GetFreeTextSearchQuery, List<FreeTextSearchResultDto>>
{
    private readonly SanctionsDbContext _db;

    public GetFreeTextSearchQueryHandler(SanctionsDbContext db)
    {
        _db = db;
    }

    public async Task<List<FreeTextSearchResultDto>> Handle(
        GetFreeTextSearchQuery request,
        CancellationToken ct)
    {
        var parts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = _db.SanctionDetails.AsQueryable();

        if (parts.Length > 1)
        {
            var first = parts[0];
            var last = parts[1];

            query = query.Where(s =>
                (EF.Functions.FreeText(s.FirstName!, first) && EF.Functions.FreeText(s.LastName, last))
                || (EF.Functions.FreeText(s.FirstName!, last) && EF.Functions.FreeText(s.LastName, first)));
        }
        else
        {
            var term = request.Name;

            query = query.Where(s =>
                EF.Functions.FreeText(s.FirstName!, term)
                || EF.Functions.FreeText(s.LastName, term));
        }

        var results = await query.ToListAsync(ct);

        return results.Select(r => r.ToFacet<FreeTextSearchResultDto>()).ToList();
    }
}