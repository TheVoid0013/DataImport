using DataImport.Data.Data;
using DataImport.Data.Models;
using DataImport.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using MethodTimer;

namespace DataImport.Commands
{
    public record ImportOfacSdnDataCommand : IRequest<ImportOfacSdnDataResult>;

    public record ImportOfacSdnDataResult(int TotalParsed, int Inserted, int Updated, int Unchanged);


    public class ImportOfacSdnDataCommandHandler : IRequestHandler<ImportOfacSdnDataCommand, ImportOfacSdnDataResult>
    {
        private readonly IMediator _mediator;
        private readonly SanctionsDbContext _db;
        private readonly ILogger<ImportOfacSdnDataCommandHandler> _logger;
        private readonly IImportFailureNotifier _notifier;

        public ImportOfacSdnDataCommandHandler(
            IMediator mediator,
            SanctionsDbContext db,
            ILogger<ImportOfacSdnDataCommandHandler> logger,
            IImportFailureNotifier notifier)
        {
            _mediator = mediator;
            _db = db;
            _logger = logger;
            _notifier = notifier;
        }

        [Time]
        public async Task<ImportOfacSdnDataResult> Handle(ImportOfacSdnDataCommand request, CancellationToken cancellationToken)
        {
            var log = new DataImportLog { RanAtUtc = DateTime.UtcNow };
            Exception? capturedException = null;

            try
            {
                try
                {
                    await _mediator.Send(new CleanupImportCacheCommand(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cache cleanup failed; import will continue.");
                }


                var download = await _mediator.Send(new DownloadSdnXmlCommand(), cancellationToken);
                log.WasDownloaded = download.WasDownloaded;

                await using var rawXml = download.Content;

                var validatedStream = await _mediator.Send(
                        new LoadAndValidateSdnXmlCommand(rawXml),
                        cancellationToken);

                var records = await _mediator.Send(
                    new ParseSdnXmlCommand(validatedStream),
                    cancellationToken);


                var saveResult = await _mediator.Send(new SaveSanctionDetailsCommand(records), cancellationToken);

                log.Parsed = records.Count;
                log.Inserted = saveResult.Inserted;
                log.Updated = saveResult.Updated;
                log.Unchanged = saveResult.Unchanged;
                log.Succeeded = true;

                return new ImportOfacSdnDataResult(records.Count, saveResult.Inserted, saveResult.Updated, saveResult.Unchanged);
            }
            catch (Exception ex)
            {
                log.Succeeded = false;
                log.ErrorMessage = ex.Message;
                capturedException = ex;
                throw;
            }
            finally
            {
                _db.DataImportLogs.Add(log);
                await _db.SaveChangesAsync(cancellationToken);

                if (!log.Succeeded && capturedException is not null)
                {
                    // Fire-and-forget-safe: notifier swallows its own exceptions internally.
                    await _notifier.NotifyAsync("OFAC SDN Import", capturedException, cancellationToken);
                }
            }
        }
    }
}