using MediatR;
using DataImport.Presentation.GenericDTO;

namespace DataImport.API.Queries;

public record GetSanctionByIdQuery(Guid Id)
    : IRequest<SanctionDetailDto?>;

public record GetSanctionsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? SdnType = null,
    string? LastNameContains = null
) : IRequest<PagedResult<SanctionListItemDto>>;