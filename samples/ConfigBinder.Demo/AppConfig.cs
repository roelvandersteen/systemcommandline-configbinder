using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ConfigBinder.Demo;

/// <summary>
///     Configuration class that contains all command line parameters.
///     Properties in this class are automatically converted to command line options.
/// </summary>
public sealed class AppConfig
{
    [Required][Display(Description = "The Cosmos DB endpoint.")] public string Endpoint { get; set; } = string.Empty;

    [Display(Description = "The database name.")] public string Database { get; set; } = "SubscriptionsDb";

    [Display(Description = "Do not persist changes.")] public bool DryRun { get; set; } = true;

    [Display(Description = "The logging level to use.")] public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [Display(Description = "The maximum number of retries.")]
    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    [Display(Description = "List of container names to process. Can be specified multiple times.")]
    public string[] ContainerNames { get; set; } = [];

    [Display(Description = "List of partition key values to filter by.")] public int[] PartitionKeys { get; set; } = [];

    [Display(Description = "Configuration file path.")] public FileInfo? ConfigFile { get; set; }

    [Display(Description = "Optional timeout in seconds.")] public int? TimeoutSeconds { get; set; }

    [Display(Description = "Optional connection string.")] public string? ConnectionString { get; set; }

    [Display(Description = "The output format for results.")] public OutputFormat OutputFormat { get; set; }
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum OutputFormat
{
    Json,
    Xml,
    Csv,
    Table
}