using DataImport.API.Queries;
using DataImport.Data.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DataImport.API.Commands;

public class GetCountryCountQueryCommand : IRequestHandler<GetCountryCountQuery, object>
{
    private readonly SanctionsDbContext _db;
    
    public  GetCountryCountQueryCommand(SanctionsDbContext db)
    {
        _db = db;
    }

    public async Task<object> Handle(GetCountryCountQuery request
        , CancellationToken ct)
    {
        var count = await _db.SanctionDetails
            .Where(x=> x.Country == request.country.ToString())
            .CountAsync(ct);

        return new
        {
            Success = true,
            Countrry = request.country,
            Count = count
        };

    }
    
}