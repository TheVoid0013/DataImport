using DataImport.Presentation.GenericDTO;
using MediatR;

namespace DataImport.Commands.Queries;

public record GetTopCountriesQuery(int Top = 6) : IRequest<TopCountriesDto>;
public record GetSdnTypeCountsQuery : IRequest<List<SdnTypeCountDto>>;