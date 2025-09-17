using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SystemCommandLine.ConfigBinder;

namespace ConfigBinder.Demo;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configBinder = new AutoConfigBinder<AppConfig>();

        var rootCommand = new RootCommand("Sample Application: Automatically Bind Options to Configuration Object");
        configBinder.AddOptionsTo(rootCommand);

        // Dependency injection is not needed, but shown here for completeness.
        ServiceProvider serviceProvider = BuildServiceProvider();

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var processor = serviceProvider.GetRequiredService<IAppProcessor>();
            try
            {
                AppConfig config = configBinder.Get(parseResult);
                return await processor.ProcessAsync(config, cancellationToken);
            }
            catch (ValidationException ex)
            {
                StringBuilder sb = new StringBuilder("Validation error: ").AppendLine(ex.Message)
                    .Append("Use --help to see valid ranges and options.");
                await Console.Error.WriteLineAsync(sb, cancellationToken);
                return 2;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder("An error occurred during processing: ").Append(ex.Message);
                await Console.Error.WriteLineAsync(sb, cancellationToken);
                return 1;
            }
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAppProcessor, AppProcessor>();
        services.AddSingleton<ILogger>(_ => new LoggerConfiguration().WriteTo.Console().CreateLogger());

        return services.BuildServiceProvider();
    }
}