using DataImport.Data.Data;
using DataImport.Data.Models;
using MediatR;
using MethodTimer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataImport.Commands
{
    /// <summary>
    /// Upserts parsed SanctionDetail records: inserts new ones, updates changed
    /// ones, leaves unchanged ones alone, reactivates relisted ones, and
    /// soft-deletes (IsActive = false) records that are no longer in the file.
    /// Processes in batches so a run of ~18k records doesn't sit in one giant
    /// transaction/timeout.
    /// </summary>
    public record SaveSanctionDetailsCommand(List<SanctionDetail> Records) : IRequest<SaveSanctionDetailsResult>;

    public record SaveSanctionDetailsResult(int Inserted, int Updated, int Unchanged, int Removed);


    public class SaveSanctionDetailsCommandHandler : IRequestHandler<SaveSanctionDetailsCommand, SaveSanctionDetailsResult>
    {
        private const int BatchSize = 500;
        private const int DefaultMaxRemovalsPerRun = 500;
        private const int MaxUidsToLog = 50;

        private readonly SanctionsDbContext _db;
        private readonly ILogger<SaveSanctionDetailsCommandHandler> _logger;
        private readonly int _maxRemovalsPerRun;

        public SaveSanctionDetailsCommandHandler(
            SanctionsDbContext db,
            ILogger<SaveSanctionDetailsCommandHandler> logger,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _maxRemovalsPerRun = configuration.GetValue("Import:MaxRemovalsPerRun", DefaultMaxRemovalsPerRun);
        }

        [Time]
        public async Task<SaveSanctionDetailsResult> Handle(SaveSanctionDetailsCommand request, CancellationToken cancellationToken)
        {
            if (request.Records.Count == 0)
            {
                throw new InvalidOperationException("SaveSanctionDetailsCommand received 0 records; refusing to proceed.");
            }

            _logger.LogInformation("Loading existing SDN records for comparison...");

            var existingByUid = await _db.SanctionDetails
                .AsNoTracking()
                .ToDictionaryAsync(d => d.RecordUniqueId, cancellationToken);

            _logger.LogInformation("Loaded {Count} existing records.", existingByUid.Count);

            // ---- Work out removals BEFORE writing anything ----
            var fileUids = request.Records
                .Select(r => r.RecordUniqueId)
                .ToHashSet(StringComparer.Ordinal);

            var toRemove = existingByUid.Values
                .Where(d => d.IsActive && !fileUids.Contains(d.RecordUniqueId))
                .Select(d => d.RecordUniqueId)
                .ToList();

            if (toRemove.Count > _maxRemovalsPerRun)
            {
                // A truncated/bad file looks identical to mass delisting. Legitimate mass
                // removals (e.g. a program terminated) do happen: verify against OFAC's
                // Recent Actions, then raise Import:MaxRemovalsPerRun and rerun.
                throw new InvalidOperationException(
                    $"{toRemove.Count} records would be delisted, exceeding the limit of {_maxRemovalsPerRun}. " +
                    "Aborting before any changes were made. Verify against OFAC Recent Actions.");
            }

            // ---- Upsert ----
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            int inserted = 0, updated = 0, unchanged = 0;
            var processed = 0;
            var relisted = new List<string>();

            try
            {
                foreach (var batch in request.Records.Chunk(BatchSize))
                {
                    foreach (var record in batch)
                    {
                        if (existingByUid.TryGetValue(record.RecordUniqueId, out var existingRecord))
                        {
                            var isRelisted = !existingRecord.IsActive;

                            if (isRelisted
                                || existingRecord.XmlRecord != record.XmlRecord
                                || existingRecord.Country != record.Country
                                || existingRecord.LastName != record.LastName
                                || existingRecord.FirstName != record.FirstName
                                || existingRecord.SdnType != record.SdnType)
                            {
                                // Attach BEFORE mutating: EF snapshots the current (old) values on
                                // attach, so DetectChanges below can flag only the columns that
                                // really changed. A pure relist then updates just IsActive /
                                // RemovedAtUtc / ImportedAtUtc and never rewrites XmlRecord or the
                                // full-text indexed name columns.
                                _db.Attach(existingRecord);

                                if (isRelisted)
                                {
                                    relisted.Add(record.RecordUniqueId);
                                }

                                existingRecord.XmlRecord = record.XmlRecord;
                                existingRecord.Country = record.Country;
                                existingRecord.ImportedAtUtc = DateTime.UtcNow;
                                existingRecord.LastName = record.LastName;
                                existingRecord.FirstName = record.FirstName;
                                existingRecord.SdnType = record.SdnType;
                                existingRecord.IsActive = true;
                                existingRecord.RemovedAtUtc = null;

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

                    // AutoDetectChanges is off, so run it once per batch. Without this call EF
                    // would not see the property changes above and nothing would be saved.
                    _db.ChangeTracker.DetectChanges();
                    await _db.SaveChangesAsync(cancellationToken);
                    _db.ChangeTracker.Clear();

                    processed += batch.Length;
                    _logger.LogInformation("Saved {Processed}/{Total} records...", processed, request.Records.Count);
                }
            }
            finally
            {
                // The context is shared with the orchestrator (DataImportLog save), so restore this.
                _db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            if (relisted.Count > 0)
            {
                _logger.LogWarning(
                    "{Count} previously delisted SDN records were relisted (showing up to {Max}): {Uids}",
                    relisted.Count,
                    MaxUidsToLog,
                    string.Join(", ", relisted.Take(MaxUidsToLog)));
            }

            // ---- Sweep: only reached if the whole upsert succeeded ----
            if (toRemove.Count > 0)
            {
                var removedAtUtc = DateTime.UtcNow;

                foreach (var chunk in toRemove.Chunk(BatchSize))
                {
                    await _db.SanctionDetails
                        .Where(d => d.IsActive && chunk.Contains(d.RecordUniqueId))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.IsActive, false)
                            .SetProperty(d => d.RemovedAtUtc, removedAtUtc),
                            cancellationToken);
                }

                // Bounded by the threshold above, so safe to log in full for audit.
                _logger.LogWarning(
                    "Delisted {Count} SDN records no longer present in file: {Uids}",
                    toRemove.Count,
                    string.Join(", ", toRemove));
            }

            return new SaveSanctionDetailsResult(inserted, updated, unchanged, toRemove.Count);
        }
    }
}