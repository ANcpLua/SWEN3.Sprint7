using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace SWEN3.Sprint7.Tests.Utils;

/// <summary>
///     Helper extensions for building the test configuration.
///     <para>Use <see cref="AddTestConfiguration"/> to combine appsettings.Test.json with in-memory overrides.</para>
///     <para>Use <see cref="CreateInMemoryVariables"/> to add only the in-memory overrides.</para>
/// </summary>
public static class TestConfigurationExtensions
{
    /// <summary>
    ///     Combines appsettings.Test.json with in-memory overrides for database and folders.
    /// </summary>
    /// <param name="builder">The configuration builder being extended.</param>
    /// <param name="postgresConnectionString">The PostgreSQL connection string (e.g. from Testcontainers).</param>
    /// <param name="testRoot">The root path used to create input/archive/error subfolders.</param>
    /// <returns>The same configuration builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     var config = new ConfigurationBuilder()
    ///         .AddTestConfiguration(pg.GetConnectionString(), testRoot)
    ///         .Build();
    ///     </code>
    /// </example>
    public static IConfigurationBuilder AddTestConfiguration(this IConfigurationBuilder builder,
        string postgresConnectionString, string testRoot)
    {
        return builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Test.json", optional: true)
            .CreateInMemoryVariables(postgresConnectionString, testRoot);
    }

    private static IConfigurationBuilder CreateInMemoryVariables(this IConfigurationBuilder builder,
        string postgresConnectionString, string testRoot)
    {
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PaperlessDb"] = postgresConnectionString,
            ["BatchFileService:InputFolderPath"] = Path.Combine(testRoot, "input"),
            ["BatchFileService:ArchiveFolderPath"] = Path.Combine(testRoot, "archive"),
            ["BatchFileService:ErrorFolderPath"] = Path.Combine(testRoot, "error")
        };

        return builder.AddInMemoryCollection(overrides);
    }
}

/// <summary>
///     Service registration helpers used by integration tests.
///     <para>Registers EF Core DbContextFactory, TickerQ (with EF operational store), logging, the batch job, and binds <see cref="Sprint7.BatchFileServiceOptions"/> from configuration.</para>
/// </summary>
public static class TestServiceCollectionExtensions
{
    /// <summary>
    ///     Adds test-time services: DbContextFactory, TickerQ with EF store, logging, the job, and options binding.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The configuration containing connection strings and BatchFileService section.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    ///     <code>
    ///     var services = new ServiceCollection()
    ///         .AddSingleton&lt;IConfiguration&gt;(config)
    ///         .CreateTickerQExtension(config)
    ///         .BuildServiceProvider();
    ///     </code>
    /// </example>
    public static IServiceCollection CreateTickerQExtension(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<BatchDbContext>(o =>
        {
            var conn = config.GetConnectionString("PaperlessDb")!;
            o.UseNpgsql(conn);
        }).AddLogging().AddTickerQ(opt =>
        {
            opt.UpdateMissedJobCheckDelay(TimeSpan.FromSeconds(30));
            opt.SetInstanceIdentifier(Environment.MachineName);
            opt.SetMaxConcurrency(Environment.ProcessorCount);
            opt.AddOperationalStore<BatchDbContext>(ef => ef.UseModelCustomizerForMigrations());
        }).AddScoped<ProcessXmlBatchJob>().AddOptionsFromSection<BatchFileServiceOptions>();

        return services;
    }
}

/// <summary>
///     Service provider helpers to bootstrap integration tests (migrations and folders).
/// </summary>
public static class TestServiceProviderExtensions
{
    /// <summary>
    ///     Runs EF Core migrations for the <see cref="BatchDbContext"/>.
    /// </summary>
    /// <param name="services">The built service provider.</param>
    /// <returns>A task that completes when migrations have been applied.</returns>
    /// <example>
    ///     <code>
    ///     await serviceProvider.CreateTestMigration();
    ///     </code>
    /// </example>
    public static async Task CreateTestMigration(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BatchDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    ///     Ensures the input, archive, and error folders exist based on <see cref="BatchFileServiceOptions"/>.
    /// </summary>
    /// <param name="services">The built service provider.</param>
    /// <example>
    ///     <code>
    ///     serviceProvider.CreateTestFolders();
    ///     </code>
    /// </example>
    public static void CreateTestFolders(this IServiceProvider services)
    {
        using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<BatchFileServiceOptions>>().Value;
        Directory.CreateDirectory(options.InputFolderPath);
        Directory.CreateDirectory(options.ArchiveFolderPath);
        Directory.CreateDirectory(options.ErrorFolderPath);
    }
}