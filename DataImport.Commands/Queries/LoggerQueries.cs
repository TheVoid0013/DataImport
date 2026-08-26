using DataImport.Presentation.GenericDTO;
using MediatR;

namespace DataImport.Commands.Queries;

public record GetQueriesPagedQuery(
    int Page = 1,
    int PageSize = 20,
    bool OrderByDescending = true
 ) : IRequest<PagedResult<ImportLogDto>>;


public record GetErrorCountQuery() : IRequest<object>;