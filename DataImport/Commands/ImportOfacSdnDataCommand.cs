using MediatR;

namespace DataImport.Commands
{
    /// <summary>
    /// Orchestrates a full OFAC SDN import: download, parse, save.
    /// This is the one you actually send from Program.cs / your scheduled entry point.
    /// </summary>
    public record ImportOfacSdnDataCommand : IRequest<ImportOfacSdnDataResult>;

    public record ImportOfacSdnDataResult(int TotalParsed, int Inserted, int Updated, int Unchanged);

    public class ImportOfacSdnDataCommandHandler : IRequestHandler<ImportOfacSdnDataCommand, ImportOfacSdnDataResult>
    {
        private readonly IMediator _mediator;

        public ImportOfacSdnDataCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<ImportOfacSdnDataResult> Handle(ImportOfacSdnDataCommand request, CancellationToken cancellationToken)
        {
            var rawXml = await _mediator.Send(new DownloadSdnXmlCommand(), cancellationToken);

            var records = await _mediator.Send(new ParseSdnXmlCommand(rawXml), cancellationToken);

            var saveResult = await _mediator.Send(new SaveSanctionDetailsCommand(records), cancellationToken);

            return new ImportOfacSdnDataResult(records.Count, saveResult.Inserted, saveResult.Updated, saveResult.Unchanged);
        }
    }
}