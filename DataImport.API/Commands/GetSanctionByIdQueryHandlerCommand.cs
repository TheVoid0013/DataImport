using DataImport.API.Queries;
using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;

namespace DataImport.API.Commands;

public class GetSanctionByIdQueryHandlerCommand
    : IRequestHandler<GetSanctionByIdQuery, SanctionDetailDto?>
{
    private readonly SanctionsDbContext _db;

    public GetSanctionByIdQueryHandlerCommand(SanctionsDbContext db)
    {
        _db = db;
    }

    public async Task<SanctionDetailDto?> Handle(
        GetSanctionByIdQuery request,
        CancellationToken ct)
    {
        var entity = await _db.SanctionDetails.FindAsync(
            new object[] { request.Id }, ct);

        return entity?.ToFacet<SanctionDetailDto>();
    }
}