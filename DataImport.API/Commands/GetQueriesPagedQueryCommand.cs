using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Data.Models;
using DataImport.Presentation.GenericDTO;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Facet.Extensions;

namespace DataImport.API.Commands;

public class GetQueriesPagedQueryCommand : IRequestHandler<GetQueriesPagedQuery, PagedResult<ImportLogDto>>
{
    private readonly SanctionsDbContext _db;

    public GetQueriesPagedQueryCommand(SanctionsDbContext db) => _db = db;

    public async Task<PagedResult<ImportLogDto>> Handle(GetQueriesPagedQuery request, CancellationToken ct)
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
    }
}