using MediatR;
using DataImport.Data.Enums;

namespace DataImport.API.Queries;


public record CountryCountRequest(Country country);


public record GetCountryCountQuery(Country country): IRequest<object>;