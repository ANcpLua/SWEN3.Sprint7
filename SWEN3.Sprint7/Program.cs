using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWEN3.Sprint7;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptionsFromSection<BatchFileServiceOptions>();

// Apply application timezone from configuration if provided to ensure cron is evaluated in expected local time
var configuredTz = builder.Configuration["BatchFileService:TimeZone"];
if (!string.IsNullOrWhiteSpace(configuredTz))
{
    try
    {
        // On Linux containers this controls local time zone used by many libraries
        Environment.SetEnvironmentVariable("TZ", configuredTz);
    }
    catch
    {
        // Non-fatal: if setting TZ fails, the app continues using default timezone (likely UTC)
    }
}

var conn = builder.Configuration.GetConnectionString("PaperlessDb") ??
           throw new InvalidOperationException("Missing ConnectionStrings:PaperlessDb");
builder.Services.AddDbContextFactory<BatchDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddTickerQ(opt =>
{
    opt.UpdateMissedJobCheckDelay(TimeSpan.FromSeconds(30));
    opt.SetInstanceIdentifier(Environment.MachineName);
    opt.SetMaxConcurrency(Environment.ProcessorCount);
    opt.AddOperationalStore<BatchDbContext>(ef =>
    {
        ef.UseModelCustomizerForMigrations();
        ef.CancelMissedTickersOnAppStart();
    });
    opt.AddDashboard(ui => { ui.BasePath = "/tickerq-dashboard"; });
});

builder.Services.AddScoped<ProcessXmlBatchJob>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BatchDbContext>>();
    await using (var db = await dbFactory.CreateDbContextAsync())
    {
        await db.Database.MigrateAsync();
    }

    var opt = scope.ServiceProvider.GetRequiredService<IOptions<BatchFileServiceOptions>>().Value;
    BatchFileService.EnsureFolders(opt);
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
    await BatchFileService.EnsureSampleIfEmptyAsync(opt, log);

    if (opt.ProcessOnStartup)
    {
        var tm = scope.ServiceProvider.GetRequiredService<ITimeTickerManager<TimeTicker>>();
        await tm.AddAsync(new TimeTicker
        {
            Function = nameof(ProcessXmlBatchJob.ProcessAccessLogs),
            ExecutionTime = DateTime.UtcNow,
            Request = [],
            Retries = 1
        });
    }
}

app.UseTickerQ();
await app.RunAsync();