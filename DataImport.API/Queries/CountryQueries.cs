using MediatR;
using DataImport.Data.Enums;
using DataImport.Presentation.GenericDTO;

namespace DataImport.API.Queries;


public record CountryCountRequest(Country country);


public record GetCountrySanctionsPagedQuery(
        Country country,
        int PageSize = 20,
        int Page = 1
    ) : IRequest<PagedResult<SanctionListItemDto>>;

public record GetCountryCountQuery(Country country): IRequest<object>;