using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace SWEN3.Sprint7;

public sealed class ProcessXmlBatchJob
{
    private readonly IDbContextFactory<BatchDbContext> _dbFactory;
    private readonly ILogger<ProcessXmlBatchJob> _log;
    private readonly IOptions<BatchFileServiceOptions> _opt;

    public ProcessXmlBatchJob(ILogger<ProcessXmlBatchJob> log, IOptions<BatchFileServiceOptions> opt,
        IDbContextFactory<BatchDbContext> dbFactory)
    {
        _log = log;
        _opt = opt;
        _dbFactory = dbFactory;
    }

    [TickerFunction(nameof(ProcessAccessLogs), "%BatchFileService:CronExpression%")]
    public async Task ProcessAccessLogs()
    {
        var o = _opt.Value;
        BatchFileService.EnsureFolders(o);

        var pattern = string.IsNullOrWhiteSpace(o.FileNamePattern) ? "*.xml" : o.FileNamePattern;

        var files = Directory.Exists(o.InputFolderPath)
            ? Directory.GetFiles(o.InputFolderPath, pattern, SearchOption.TopDirectoryOnly)
            : [];

        if (files.Length is 0)
        {
            _log.LogInformation("No files found in {Folder} (pattern {Pattern})", o.InputFolderPath, pattern);
            return;
        }

        foreach (var file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await ProcessFileAsync(file, CancellationToken.None);
                BatchFileService.MoveWithTimestamp(file, o.ArchiveFolderPath, _log, "archive");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed processing {File}", file);
                try
                {
                    BatchFileService.MoveWithTimestamp(file, o.ErrorFolderPath, _log, "error");
                }
                catch (Exception moveEx)
                {
                    _log.LogCritical(moveEx, "Fail move errored file: {File}", file);
                }
            }
        }
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken ct)
    {
        _log.LogInformation("Processing XML: {File}", Path.GetFileName(filePath));

        var (date, aggregated) = AccessStatisticsXml.ParseStreaming(filePath, ct);
        if (aggregated.Count is 0)
        {
            _log.LogWarning("No entries in {File}", Path.GetFileName(filePath));
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ids = aggregated.Keys.ToArray();

        // ----- both sides are IQueryable, so EF Core can translate LeftJoin to SQL -----
        var docWithAccess = await db.Documents.Where(d => ids.Contains(d.Id)) // IQueryable
            .LeftJoin(db.DailyDocumentAccesses.Where(a => a.LogDate == date), // IQueryable
                d => d.Id, a => a.DocumentId, (d, a) => new { Document = d, Access = a }).ToListAsync(ct);

        var validDocs = docWithAccess.Select(x => x.Document).ToDictionary(d => d.Id);
        var missingIds = ids.Except(validDocs.Keys).ToArray();

        if (validDocs.Count is 0)
        {
            _log.LogWarning("No known documents found for IDs in {File}", Path.GetFileName(filePath));
            return;
        }

        if (missingIds.Length > 0)
            _log.LogWarning("Missing document IDs in DB: {Ids}", string.Join(", ", missingIds));

        var existingDailyIds = docWithAccess.Where(x => x.Access is not null).Select(x => x.Document.Id).ToHashSet();

        var now = DateTime.UtcNow;

        foreach (var (docId, count) in aggregated)
        {
            if (!validDocs.ContainsKey(docId))
                continue;

            if (existingDailyIds.Contains(docId))
            {
                await db.DailyDocumentAccesses.Where(d => d.DocumentId == docId && d.LogDate == date)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(d => d.AccessCount, d => checked(d.AccessCount + count))
                            .SetProperty(d => d.UpdatedAt, _ => now), ct);
            }
            else
            {
                db.DailyDocumentAccesses.Add(new DailyDocumentAccess
                {
                    Id = Guid.NewGuid(),
                    DocumentId = docId,
                    LogDate = date,
                    AccessCount = count,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var added = aggregated.Keys.Count(id => validDocs.ContainsKey(id) && !existingDailyIds.Contains(id));
        var updated = aggregated.Keys.Count(id => validDocs.ContainsKey(id) && existingDailyIds.Contains(id));

        _log.LogInformation("Upserted: {Added} added, {Updated} updated for {Date} (file {File})", added, updated, date,
            Path.GetFileName(filePath));
    }
}