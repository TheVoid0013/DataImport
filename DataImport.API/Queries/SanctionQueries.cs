using MediatR;
using DataImport.Presentation.GenericDTO;

namespace DataImport.API.Queries;

public record GetSanctionByIdQuery(string Id)
    : IRequest<SanctionDetailDto?>;

public record GetSanctionsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? SdnType = null,
    string? LastNameContains = null
) : IRequest<PagedResult<SanctionListItemDto>>;

public record SearchRequest(string Name);


public record GetFreeTextSearchQuery(string Name) : IRequest<List<FreeTextSearchResultDto>>;