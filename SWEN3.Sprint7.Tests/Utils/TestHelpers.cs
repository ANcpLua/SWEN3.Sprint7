using Microsoft.EntityFrameworkCore;

namespace SWEN3.Sprint7.Tests.Utils;

public static class TestHelpers
{
    public static Document CreateDocument(string fileName = "test.pdf") => new()
    {
        Id = Guid.NewGuid(),
        FileName = fileName,
        StoragePath = $"/storage/{fileName}"
    };

    public static void WriteAccessXml(string folderPath, DateOnly date, params (Guid Id, int Count)[] entries)
    {
        var path = Path.Combine(folderPath, $"access_{date:yyyyMMdd}.xml");
        AccessStatisticsXml.WriteSample(path, date, entries);
    }

    public static void WriteAccessXml(string folderPath, DateOnly date, string suffix,
        params (Guid Id, int Count)[] entries)
    {
        var path = Path.Combine(folderPath, $"access_{date:yyyyMMdd}{suffix}.xml");
        AccessStatisticsXml.WriteSample(path, date, entries);
    }

    public static async Task CleanDatabase(IDbContextFactory<BatchDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.DailyDocumentAccesses.ExecuteDeleteAsync(ct);
        await db.Documents.ExecuteDeleteAsync(ct);
    }

    public static void CleanFolders(BatchFileServiceOptions options)
    {
        foreach (var folder in new[] { options.InputFolderPath, options.ArchiveFolderPath, options.ErrorFolderPath })
            if (Directory.Exists(folder))
                foreach (var file in Directory.GetFiles(folder))
                    File.Delete(file);
    }

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.Date);
    public static DateOnly Yesterday => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
    public static DateOnly DaysAgo(int days) => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-days));
}