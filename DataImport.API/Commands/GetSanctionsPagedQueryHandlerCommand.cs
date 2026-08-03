using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DataImport.API.Queries;

namespace DataImport.API.Commands;

    public class GetSanctionsPagedQueryHandlerCommand : IRequestHandler<GetSanctionsPagedQuery, PagedResult<SanctionListItemDto>>
    {

        private readonly SanctionsDbContext _db;
        public GetSanctionsPagedQueryHandlerCommand(SanctionsDbContext db) => _db = db;
        public async Task<PagedResult<SanctionListItemDto>> Handle(GetSanctionsPagedQuery request, CancellationToken ct)
        {
            var query = _db.SanctionDetails.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SdnType))
                query = query.Where(s => s.SdnType == request.SdnType);

            if (!string.IsNullOrWhiteSpace(request.LastNameContains))
                query = query.Where(s => s.LastName.Contains(request.LastNameContains));

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(s => s.LastName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => s.ToFacet<SanctionListItemDto>())
                .ToListAsync(ct);

            return new PagedResult<SanctionListItemDto>(items, request.Page, request.PageSize, total,
                (int)Math.Ceiling(total / (double)request.PageSize));
        }

    }

