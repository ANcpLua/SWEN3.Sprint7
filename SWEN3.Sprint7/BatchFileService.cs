using System.Globalization;

namespace SWEN3.Sprint7;

public static class BatchFileService
{
    public static void EnsureFolders(BatchFileServiceOptions o)
    {
        Directory.CreateDirectory(o.InputFolderPath);
        Directory.CreateDirectory(o.ArchiveFolderPath);
        Directory.CreateDirectory(o.ErrorFolderPath);
    }

    public static Task EnsureSampleIfEmptyAsync(BatchFileServiceOptions o, ILogger log)
    {
        if (!o.ProcessOnStartup || Directory
                .EnumerateFiles(o.InputFolderPath, o.FileNamePattern, SearchOption.TopDirectoryOnly)
                .Any()) return Task.CompletedTask;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fileName = string.Format(CultureInfo.InvariantCulture,
            AccessStatisticsXml.BatchConstants.SampleFileNameFormat, today);
        var file = Path.Combine(o.InputFolderPath, fileName);

        AccessStatisticsXml.WriteSample(file, today, [(Guid.NewGuid(), 5), (Guid.NewGuid(), 13)]);
        log.LogInformation("Sample XML created: {File}", file);

        return Task.CompletedTask;
    }

    public static void MoveWithTimestamp(string source, string destFolder, ILogger log, string tag)
    {
        Directory.CreateDirectory(destFolder);
        var dest = Path.Combine(destFolder, $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}_{Path.GetFileName(source)}");
        File.Move(source, dest, false);
        log.LogInformation("Moved {Src} -> {Dest} ({Tag})", source, dest, tag);
    }
}