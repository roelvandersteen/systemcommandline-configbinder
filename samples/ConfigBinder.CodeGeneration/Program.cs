using System.CommandLine;
using ConfigBinder.CodeGeneration;
using System.Text;

var root = new RootCommand("Sample tool");
AppConfigOptions.AddOptionsTo(root);

root.SetAction(async (parseResult, cancellationToken) =>
{
    AppConfig config = AppConfigOptions.Get(parseResult);
    await Console.Out.WriteLineAsync(new StringBuilder("Retries: ").Append(config.Retries), cancellationToken);
    await Task.Delay(200, cancellationToken);
    return 0;
});
return await root.Parse(args).InvokeAsync();