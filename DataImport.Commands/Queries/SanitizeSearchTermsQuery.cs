using MediatR;

namespace DataImport.Commands.Queries;

public record SanitizeSearchTermsQuery(string Name) : IRequest<string[]>;