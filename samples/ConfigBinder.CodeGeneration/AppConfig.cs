using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ConfigBinder.CodeGeneration;

/// <summary>
///     Configuration class that contains all command line parameters.
///     Properties in this class are automatically converted to command line options.
/// </summary>
public sealed class AppConfig
{
    [Required]
    [Display(Description = "The service endpoint.")]
    public string Endpoint { get; set; } = string.Empty;

    [Display(Description = "The database name.")]
    public string Database { get; set; } = "DefaultDb";

    [Display(Description = "Do not persist changes.")]
    public bool DryRun { get; set; } = true;

    [Display(Description = "The logging level to use.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [Display(Description = "The maximum number of retries.")]
    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    [Display(Description = "The output format for results.")]
    public OutputFormat OutputFormat { get; set; }

    [Display(Description = "Enable verbose logging.")]
    public bool Verbose { get; set; } = false;

    [Display(Description = "The connection timeout in seconds.")]
    [Range(5, 300, ErrorMessage = "Timeout must be between 5 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
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