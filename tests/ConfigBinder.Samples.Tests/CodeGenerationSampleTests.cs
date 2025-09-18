using System.CommandLine;
using ConfigBinder.CodeGeneration;

namespace ConfigBinder.Samples.Tests;

public class CodeGenerationSampleTests
{
    [Fact]
    public void AppConfigOptions_AddOptionsTo_ShouldAddAllOptions()
    {
        // Arrange
        var command = new RootCommand();

        // Act
        AppConfigOptions.AddOptionsTo(command);

        // Assert - The command will include both positive and negative options for booleans
        Assert.True(command.Options.Count >= 8); // At least the basic 8 options
        Assert.Contains(command.Options, o => o.Name == "--endpoint");
        Assert.Contains(command.Options, o => o.Name == "--database");
        Assert.Contains(command.Options, o => o.Name == "--dry-run");
        Assert.Contains(command.Options, o => o.Name == "--log-level");
        Assert.Contains(command.Options, o => o.Name == "--max-retries");
        Assert.Contains(command.Options, o => o.Name == "--output-format");
        Assert.Contains(command.Options, o => o.Name == "--verbose");
        Assert.Contains(command.Options, o => o.Name == "--timeout-seconds");
    }

    [Fact]
    public void AppConfigOptions_Get_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[]
        {
            "--endpoint",
            "https://example.com",
            "--max-retries",
            "5",
            "--database",
            "TestDb"
        };

        // Act
        ParseResult parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://example.com", config.Endpoint);
        Assert.Equal("TestDb", config.Database);
        Assert.True(config.DryRun); // Default value
        Assert.Equal(LogLevel.Trace, config.LogLevel); // Generator doesn't support enum defaults yet - uses first enum value
        Assert.Equal(5, config.MaxRetries);
        Assert.Equal(OutputFormat.Json, config.OutputFormat); // Generator doesn't support enum defaults yet - uses first enum value
        Assert.False(config.Verbose); // Default value
        Assert.Equal(30, config.TimeoutSeconds); // Default value
    }

    [Fact]
    public void AppConfigOptions_Get_WithEnumValues_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[]
        {
            "--endpoint",
            "https://test.com",
            "--log-level",
            "Debug",
            "--output-format",
            "Xml",
            "--verbose",
            "--timeout-seconds",
            "60"
        };

        // Act
        ParseResult parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://test.com", config.Endpoint);
        Assert.Equal(LogLevel.Debug, config.LogLevel);
        Assert.Equal(OutputFormat.Xml, config.OutputFormat);
        Assert.True(config.Verbose);
        Assert.Equal(60, config.TimeoutSeconds);
    }

    [Fact]
    public void AppConfigOptions_Get_WithInvalidRetries_ShouldNotThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://example.com", "--max-retries", "15" }; // Out of range
        ParseResult parseResult = command.Parse(args);

        // Act - The validation happens at the model level, not during parsing
        var config = AppConfigOptions.Get(parseResult);

        // Assert - Value is set even if out of range, validation would happen elsewhere
        Assert.NotNull(config);
        Assert.Equal(15, config.MaxRetries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithMissingRequiredEndpoint_ShouldThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--max-retries", "5" }; // Missing required endpoint
        ParseResult parseResult = command.Parse(args);

        // Act & Assert - Should throw InvalidOperationException for missing required option
        Assert.Throws<InvalidOperationException>(() => AppConfigOptions.Get(parseResult));
    }

    [Fact]
    public void AppConfigOptions_ShouldHaveCorrectOptions()
    {
        // Arrange & Act - Test that the generated options exist and have correct types
        var endpointOption = AppConfigOptions.EndpointOption;
        var databaseOption = AppConfigOptions.DatabaseOption;
        var dryRunOption = AppConfigOptions.DryRunOption;
        var logLevelOption = AppConfigOptions.LogLevelOption;
        var maxRetriesOption = AppConfigOptions.MaxRetriesOption;
        var outputFormatOption = AppConfigOptions.OutputFormatOption;
        var verboseOption = AppConfigOptions.VerboseOption;
        var timeoutSecondsOption = AppConfigOptions.TimeoutSecondsOption;

        // Assert
        Assert.NotNull(endpointOption);
        Assert.NotNull(databaseOption);
        Assert.NotNull(dryRunOption);
        Assert.NotNull(logLevelOption);
        Assert.NotNull(maxRetriesOption);
        Assert.NotNull(outputFormatOption);
        Assert.NotNull(verboseOption);
        Assert.NotNull(timeoutSecondsOption);

        Assert.Equal("--endpoint", endpointOption.Name);
        Assert.Equal("--database", databaseOption.Name);
        Assert.Equal("--dry-run", dryRunOption.Name);
        Assert.Equal("--log-level", logLevelOption.Name);
        Assert.Equal("--max-retries", maxRetriesOption.Name);
        Assert.Equal("--output-format", outputFormatOption.Name);
        Assert.Equal("--verbose", verboseOption.Name);
        Assert.Equal("--timeout-seconds", timeoutSecondsOption.Name);

        Assert.True(endpointOption.Required);
        Assert.False(databaseOption.Required);
        Assert.False(dryRunOption.Required);
        Assert.False(logLevelOption.Required);
        Assert.False(maxRetriesOption.Required);
        Assert.False(outputFormatOption.Required);
        Assert.False(verboseOption.Required);
        Assert.False(timeoutSecondsOption.Required);
    }
}