namespace DataImport.API.Commands;

using DataImport.Data.Data;
using DataImport.Presentation.GenericDTO;
using Facet.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DataImport.Commands.Queries;

public class GetErrorCountQueryCommand : IRequestHandler<GetErrorCountQuery, object>
{
    private readonly SanctionsDbContext _db;
    public GetErrorCountQueryCommand(SanctionsDbContext db) => _db = db;

    public async Task<Object> Handle(GetErrorCountQuery request, CancellationToken ct)
    {
        var count = await _db.DataImportLogs
            .Where(x => !x.Succeeded)
            .CountAsync(ct);

        return new { 
            success=true,
            errorCount = count
        };
    }
}

