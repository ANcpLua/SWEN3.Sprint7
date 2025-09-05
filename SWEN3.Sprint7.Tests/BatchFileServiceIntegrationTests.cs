using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SWEN3.Sprint7.Tests.Utils;

namespace SWEN3.Sprint7.Tests;

public sealed class BatchFileServiceIntegrationTests : IClassFixture<BatchFileServiceFixture>, IAsyncLifetime
{
    private readonly AsyncServiceScope _scope;
    private readonly IDbContextFactory<BatchDbContext> _dbFactory;
    private readonly BatchFileServiceOptions _options;
    private readonly ProcessXmlBatchJob _job;

    public BatchFileServiceIntegrationTests(BatchFileServiceFixture fixture)
    {
        _scope = fixture.Services.CreateAsyncScope();
        _dbFactory = _scope.ServiceProvider.GetRequiredService<IDbContextFactory<BatchDbContext>>();
        _options = _scope.ServiceProvider.GetRequiredService<IOptions<BatchFileServiceOptions>>().Value;
        _job = _scope.ServiceProvider.GetRequiredService<ProcessXmlBatchJob>();
    }

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await TestHelpers.CleanDatabase(_dbFactory, ct);
        TestHelpers.CleanFolders(_options);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    [Fact]
    public async Task ProcessAccessLogs_WithValidXmlFile_ShouldPersistToDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        var document1 = TestHelpers.CreateDocument("report.pdf");
        var document2 = TestHelpers.CreateDocument("invoice.pdf");

        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            db.Documents.AddRange(document1, document2);
            await db.SaveChangesAsync(ct);
        }

        TestHelpers.WriteAccessXml(_options.InputFolderPath, TestHelpers.Today, (document1.Id, 5), (document2.Id, 10));

        await _job.ProcessAccessLogs();

        await using var db2 = await _dbFactory.CreateDbContextAsync(ct);
        var accessRecords = await db2.DailyDocumentAccesses.Where(a => a.LogDate == TestHelpers.Today).ToListAsync(ct);

        accessRecords.Should().HaveCount(2);
        accessRecords.Should().Contain(r => r.DocumentId == document1.Id && r.AccessCount == 5);
        accessRecords.Should().Contain(r => r.DocumentId == document2.Id && r.AccessCount == 10);

        Directory.GetFiles(_options.InputFolderPath).Should().BeEmpty("files should be moved after processing");
        Directory.GetFiles(_options.ArchiveFolderPath).Should().HaveCount(1, "file should be archived");
    }

    [Fact]
    public async Task ProcessAccessLogs_WithDuplicateProcessing_ShouldAggregateCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var document = TestHelpers.CreateDocument("report.pdf");

        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            db.Documents.Add(document);
            await db.SaveChangesAsync(ct);
        }

        TestHelpers.WriteAccessXml(_options.InputFolderPath, TestHelpers.Yesterday, "_batch1", (document.Id, 3));
        TestHelpers.WriteAccessXml(_options.InputFolderPath, TestHelpers.Yesterday, "_batch2", (document.Id, 7));

        await _job.ProcessAccessLogs();

        await using var db2 = await _dbFactory.CreateDbContextAsync(ct);
        var accessRecord = await db2.DailyDocumentAccesses.SingleAsync(a => a.DocumentId == document.Id, ct);

        accessRecord.AccessCount.Should().Be(10, "counts should be aggregated (3 + 7)");
        accessRecord.LogDate.Should().Be(TestHelpers.Yesterday);

        Directory.GetFiles(_options.ArchiveFolderPath).Should().HaveCount(2, "both files should be archived");
    }

    [Fact]
    public async Task ProcessAccessLogs_WithInvalidDocumentIds_ShouldSkipInvalidEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        var validDocument = TestHelpers.CreateDocument("valid.pdf");
        var invalidDocumentId = Guid.NewGuid();

        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            db.Documents.Add(validDocument);
            await db.SaveChangesAsync(ct);
        }

        TestHelpers.WriteAccessXml(_options.InputFolderPath, TestHelpers.DaysAgo(2), (validDocument.Id, 5),
            (invalidDocumentId, 10));

        await _job.ProcessAccessLogs();

        await using var db2 = await _dbFactory.CreateDbContextAsync(ct);
        var accessRecords = await db2.DailyDocumentAccesses.ToListAsync(ct);

        accessRecords.Should().ContainSingle("only valid document should be processed");
        accessRecords[0].DocumentId.Should().Be(validDocument.Id);
        accessRecords[0].AccessCount.Should().Be(5);
    }

    [Fact]
    public async Task ProcessAccessLogs_WithEmptyFolder_ShouldHandleGracefully()
    {
        await _job.ProcessAccessLogs();

        var ct = TestContext.Current.CancellationToken;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var accessRecords = await db.DailyDocumentAccesses.ToListAsync(ct);

        accessRecords.Should().BeEmpty("no files were processed");
        Directory.GetFiles(_options.ArchiveFolderPath).Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 15)]
    [InlineData(7, 35)]
    public async Task ProcessAccessLogs_WithVariousDaysAgo_ShouldProcessCorrectly(int daysAgo, int accessCount)
    {
        var ct = TestContext.Current.CancellationToken;
        var document = TestHelpers.CreateDocument($"report_day{daysAgo}.pdf");
        var logDate = TestHelpers.DaysAgo(daysAgo);

        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            db.Documents.Add(document);
            await db.SaveChangesAsync(ct);
        }

        TestHelpers.WriteAccessXml(_options.InputFolderPath, logDate, (document.Id, accessCount));

        await _job.ProcessAccessLogs();

        await using var db2 = await _dbFactory.CreateDbContextAsync(ct);
        var accessRecord = await db2.DailyDocumentAccesses.SingleAsync(a => a.DocumentId == document.Id, ct);

        accessRecord.AccessCount.Should().Be(accessCount);
        accessRecord.LogDate.Should().Be(logDate);
    }
}