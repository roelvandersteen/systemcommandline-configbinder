using Serilog;

namespace ConfigBinder.Reflection;

public class AppProcessor(ILogger logger) : IAppProcessor
{
    public async Task<int> ProcessAsync(AppConfig config, CancellationToken cancellationToken)
    {
#pragma warning disable S6664 // Log the configuration values (for demonstration purposes)
        logger.Information("Starting tool with configuration:");
        logger.Information("  DryRun: {DryRun}", config.DryRun);
        logger.Information("  Database: {Database}", config.Database);
        logger.Information("  Endpoint: {Endpoint}", config.Endpoint);
        logger.Information("  LogLevel: {LogLevel}", config.LogLevel);
        logger.Information("  MaxRetries: {MaxRetries}", config.MaxRetries);
        logger.Information("  ContainerNames: {ContainerNames}", string.Join(",", config.ContainerNames));
        logger.Information("  PartitionKeys: {PartitionKeys}", string.Join(",", config.PartitionKeys));
        logger.Information("  ConfigFile: {ConfigFile}", config.ConfigFile?.FullName ?? "none");
        logger.Information("  TimeoutSeconds: {TimeoutSeconds}", config.TimeoutSeconds?.ToString() ?? "none");
        logger.Information("  ConnectionString: {ConnectionString}", config.ConnectionString ?? "none");
        logger.Information("  OutputFormat: {OutputFormat}", config.OutputFormat);
#pragma warning restore S6664

        // Simulate some work
        await Task.Delay(200, cancellationToken);

        logger.Information("Completed successfully");
        return 0;
    }
}