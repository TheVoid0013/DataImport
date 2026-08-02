using DataImport.Data.Data;
using DataImport.Data.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    /// <summary>
    /// Upserts parsed SanctionDetail records: inserts new ones, updates changed
    /// ones, leaves unchanged ones alone. Processes in batches so a run of
    /// ~18k records doesn't sit in one giant transaction/timeout.
    /// </summary>
    public record SaveSanctionDetailsCommand(List<SanctionDetail> Records) : IRequest<SaveSanctionDetailsResult>;

    public record SaveSanctionDetailsResult(int Inserted, int Updated, int Unchanged);

    public class SaveSanctionDetailsCommandHandler : IRequestHandler<SaveSanctionDetailsCommand, SaveSanctionDetailsResult>
    {
        private const int BatchSize = 500;

        private readonly SanctionsDbContext _db;
        private readonly ILogger<SaveSanctionDetailsCommandHandler> _logger;

        public SaveSanctionDetailsCommandHandler(SanctionsDbContext db, ILogger<SaveSanctionDetailsCommandHandler> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<SaveSanctionDetailsResult> Handle(SaveSanctionDetailsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading existing SDN records for comparison...");

            var existingByUid = await _db.SanctionDetails
                .AsNoTracking()
                .ToDictionaryAsync(d => d.RecordUniqueId, cancellationToken);

            _logger.LogInformation("Loaded {Count} existing records.", existingByUid.Count);

            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            int inserted = 0, updated = 0, unchanged = 0;
            var processed = 0;

            foreach (var batch in request.Records.Chunk(BatchSize))
            {
                foreach (var record in batch)
                {
                    if (existingByUid.TryGetValue(record.RecordUniqueId, out var existingRecord))
                    {
                        if (existingRecord.XmlRecord != record.XmlRecord || existingRecord.Country != record.Country)
                        {
                            existingRecord.XmlRecord = record.XmlRecord;
                            existingRecord.Country = record.Country;
                            existingRecord.ImportedAtUtc = DateTime.UtcNow;

                            _db.Attach(existingRecord);
                            _db.Entry(existingRecord).State = EntityState.Modified;

                            updated++;
                        }
                        else
                        {
                            unchanged++;
                        }
                    }
                    else
                    {
                        _db.SanctionDetails.Add(record);
                        inserted++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                _db.ChangeTracker.Clear();

                processed += batch.Length;
                _logger.LogInformation("Saved {Processed}/{Total} records...", processed, request.Records.Count);
            }

            return new SaveSanctionDetailsResult(inserted, updated, unchanged);
        }
    }
}