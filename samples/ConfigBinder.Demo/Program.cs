using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ConfigBinder.Demo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SystemCommandLine.ConfigBinder;

var configBinder = new AutoConfigBinder<AppConfig>();

var rootCommand = new RootCommand("Sample Application: Automatically Bind Options to Configuration Object");
configBinder.AddOptionsTo(rootCommand);

// Dependency injection is not needed, but shown here for completeness.
var serviceProvider = BuildServiceProvider();

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var processor = serviceProvider.GetRequiredService<IAppProcessor>();
    try
    {
        var config = configBinder.Get(parseResult);
        return await processor.ProcessAsync(config, cancellationToken);
    }
    catch (ValidationException ex)
    {
        var sb = new StringBuilder("Validation error: ").AppendLine(ex.Message).Append("Use --help to see valid ranges and options.");
        await Console.Error.WriteLineAsync(sb);
        return 2;
    }
    catch (Exception ex)
    {
        var sb = new StringBuilder("An error occurred during processing: ").Append(ex.Message);
        await Console.Error.WriteLineAsync(sb);
        return 1;
    }
});

return await rootCommand.Parse(args).InvokeAsync();

static ServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();
    services.AddSingleton<IAppProcessor, AppProcessor>();
    services.AddSingleton<ILogger>(_ => new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger());

    return services.BuildServiceProvider();
}