using MediatR;
using DataImport.Data.Enums;
using DataImport.Presentation.GenericDTO;

namespace DataImport.Commands.Queries;


public record CountryCountRequest(Country country);


public record GetCountrySanctionsPagedQuery(
        Country country,
        int PageSize = 20,
        int Page = 1
    ) : IRequest<PagedResult<SanctionListItemDto>>;


public record GetDelistedCountrySanctionsPagedQuery(Country country, 
    int PageSize, 
    int Page)
    : IRequest<PagedResult<SanctionListItemDto>>;


public record GetCountryCountQuery(Country country) : IRequest<DataImport.Commands.Commands.CountryCountResult>;

public record GetCountryDelistedCountQuery(Country country) : IRequest<DataImport.Commands.Commands.CountryCountResult>;

