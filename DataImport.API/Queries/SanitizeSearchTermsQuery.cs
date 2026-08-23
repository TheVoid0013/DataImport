using MediatR;

namespace DataImport.API.Queries;

public record SanitizeSearchTermsQuery(string Name) : IRequest<string[]>;