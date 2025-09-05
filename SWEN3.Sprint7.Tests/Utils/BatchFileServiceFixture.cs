using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace SWEN3.Sprint7.Tests.Utils;

public sealed class BatchFileServiceFixture : IAsyncLifetime
{
    private static readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public IServiceProvider Services { get; private set; } = null!;
    public static string TestRoot { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        TestRoot = Path.Combine(Path.GetTempPath(), "BatchTests", Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddTestConfiguration(_postgres.GetConnectionString(), TestRoot).Build();
        Services = new ServiceCollection().AddSingleton<IConfiguration>(config)
            .CreateTickerQExtension(config)
            .BuildServiceProvider();

        await Services.CreateTestMigration();
        Services.CreateTestFolders();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();

        if (Directory.Exists(TestRoot))
            Directory.Delete(TestRoot, recursive: true);

        if (Services is IDisposable disposable)
            disposable.Dispose();
    }
}