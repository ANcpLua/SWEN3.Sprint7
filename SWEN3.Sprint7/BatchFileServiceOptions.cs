using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace SWEN3.Sprint7;

/// <summary>
///     Strongly-typed configuration for batch processing of access log XML files.
///     <para>Values are bound from the configuration section specified by <see cref="ConfigurationSectionAttribute"/>.</para>
///     <remarks>
///         This options class is used by the batch processing job and during application startup
///         to ensure input/archive/error folders exist and to optionally trigger a one-time run.
///     </remarks>
/// </summary>
[ConfigurationSection("BatchFileService")]
public sealed class BatchFileServiceOptions
{
    [Required, Description("Path where external systems drop XML files.")]
    public required string InputFolderPath { get; set; }

    [Required, Description("Path where successfully processed files are archived.")]
    public required string ArchiveFolderPath { get; set; }

    [Required, Description("Path where files are moved if processing fails.")]
    public required string ErrorFolderPath { get; set; }

    [Description("Glob pattern to select files, e.g. access_*.xml")]
    public required string FileNamePattern { get; set; }

    [Description("If true, run once immediately on startup (used by integration tests).")]
    public bool ProcessOnStartup { get; set; }
}

/// <summary>
///     Registers <typeparamref name="TOptions"/> to be bound from configuration using the path defined by
///     its <see cref="ConfigurationSectionAttribute"/>, validates data annotations, and validates on app start.
///     This overload relies on the application's <see cref="IConfiguration"/> from the DI container and
///     avoids passing <see cref="IConfiguration"/> explicitly.
/// </summary>
/// <typeparam name="TOptions">The options type to configure.</typeparam>
/// <param name="services">The service collection.</param>
/// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further customization.</returns>
/// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOptions"/> lacks the attribute.</exception>
/// <example>
///     <code>
///     // Binds options from the section name specified on the type via [ConfigurationSection]
///     builder.Services.AddOptionsFromSection&lt;BatchFileServiceOptions&gt;();
///     </code>
/// </example>
public static class OptionsRegistrationExtensions
{
    public static OptionsBuilder<TOptions> AddOptionsFromSection<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        var attr = typeof(TOptions).GetCustomAttribute<ConfigurationSectionAttribute>() ??
                   throw new InvalidOperationException(
                       $"Missing [ConfigurationSection] on {typeof(TOptions).FullName}.");

        return services.AddOptionsWithValidateOnStart<TOptions>().BindConfiguration(attr.Path)
            .ValidateDataAnnotations();
    }
}