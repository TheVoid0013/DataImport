using DataImport.Data;
using DataImport.Models;
using MediatR;
using Microsoft.Extensions.Logging;

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
        private readonly SanctionsDbContext _db;
        private readonly ILogger<ImportOfacSdnDataCommandHandler> _logger;

        public ImportOfacSdnDataCommandHandler(
            IMediator mediator,
            SanctionsDbContext db,
            ILogger<ImportOfacSdnDataCommandHandler> logger)
        {
            _mediator = mediator;
            _db = db;
            _logger = logger;
        }

        public async Task<ImportOfacSdnDataResult> Handle(ImportOfacSdnDataCommand request, CancellationToken cancellationToken)
        {
            var log = new DataImportLog { RanAtUtc = DateTime.UtcNow };

            try
            {
                try
                {
                    await _mediator.Send(new CleanupImportCacheCommand(), cancellationToken);
                }
                catch (Exception ex)
                {
                    // even if Cache cleaning fails, the show must go on. 
                    _logger.LogWarning(ex, "Cache cleanup failed; import will continue.");
                }

                var download = await _mediator.Send(new DownloadSdnXmlCommand(), cancellationToken);
                log.WasDownloaded = download.WasDownloaded;

                await using var rawXml = download.Content;

                var records = await _mediator.Send(new ParseSdnXmlCommand(rawXml), cancellationToken);

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
                throw;
            }
            finally
            {
                _db.DataImportLogs.Add(log);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}