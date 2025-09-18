using System.CommandLine;
using ConfigBinder.CodeGeneration;
using System.Text;

var root = new RootCommand("Sample tool for code generation");
AppConfigOptions.AddOptionsTo(root);

root.SetAction(async (parseResult, cancellationToken) =>
{
    AppConfig config = AppConfigOptions.Get(parseResult);

    var sb = new StringBuilder();
    sb.AppendLine("Configuration loaded:");
    sb.AppendLine($"  Endpoint: {config.Endpoint}");
    sb.AppendLine($"  Database: {config.Database}");
    sb.AppendLine($"  DryRun: {config.DryRun}");
    sb.AppendLine($"  LogLevel: {config.LogLevel}");
    sb.AppendLine($"  MaxRetries: {config.MaxRetries}");
    sb.AppendLine($"  ContainerNames: {string.Join(",", config.ContainerNames)}");
    sb.AppendLine($"  PartitionKeys: {string.Join(",", config.PartitionKeys)}");
    sb.AppendLine($"  ConfigFile: {config.ConfigFile?.FullName ?? "none"}");
    sb.AppendLine($"  TimeoutSeconds: {config.TimeoutSeconds?.ToString() ?? "none"}");
    sb.AppendLine($"  ConnectionString: {config.ConnectionString ?? "none"}");
    sb.AppendLine($"  OutputFormat: {config.OutputFormat}");

    await Console.Out.WriteLineAsync(sb, cancellationToken);
    await Task.Delay(200, cancellationToken);
    return 0;
});
return await root.Parse(args).InvokeAsync();