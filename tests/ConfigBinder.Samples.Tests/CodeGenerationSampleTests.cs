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
        Assert.True(command.Options.Count >= 10); // At least the basic 10 options 
        Assert.Contains(command.Options, o => o.Name == "--endpoint");
        Assert.Contains(command.Options, o => o.Name == "--database");
        Assert.Contains(command.Options, o => o.Name == "--dry-run");
        Assert.Contains(command.Options, o => o.Name == "--log-level");
        Assert.Contains(command.Options, o => o.Name == "--max-retries");
        Assert.Contains(command.Options, o => o.Name == "--container-names");
        Assert.Contains(command.Options, o => o.Name == "--partition-keys");
        Assert.Contains(command.Options, o => o.Name == "--config-file");
        Assert.Contains(command.Options, o => o.Name == "--timeout-seconds");
        Assert.Contains(command.Options, o => o.Name == "--connection-string");
        Assert.Contains(command.Options, o => o.Name == "--output-format");
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
        Assert.True(config.DryRun);
        Assert.Equal(LogLevel.Information, config.LogLevel);
        Assert.Equal(5, config.MaxRetries);
        Assert.Equal(OutputFormat.Json, config.OutputFormat);
        Assert.Null(config.TimeoutSeconds);
        Assert.Null(config.ConnectionString);
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
        Assert.Equal(60, config.TimeoutSeconds);
    }

    [Fact]
    public void AppConfigOptions_Get_WithNullableValues_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[]
        {
            "--endpoint",
            "https://nullable-test.com",
            "--timeout-seconds",
            "250",
            "--connection-string",
            "server=test;database=mydb"
        };

        // Act
        ParseResult parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://nullable-test.com", config.Endpoint);
        Assert.Equal(250, config.TimeoutSeconds);
        Assert.Equal("server=test;database=mydb", config.ConnectionString);
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
        var containerNamesOption = AppConfigOptions.ContainerNamesOption;
        var partitionKeysOption = AppConfigOptions.PartitionKeysOption;
        var configFileOption = AppConfigOptions.ConfigFileOption;
        var timeoutSecondsOption = AppConfigOptions.TimeoutSecondsOption;
        var connectionStringOption = AppConfigOptions.ConnectionStringOption;
        var outputFormatOption = AppConfigOptions.OutputFormatOption;

        // Assert
        Assert.NotNull(endpointOption);
        Assert.NotNull(databaseOption);
        Assert.NotNull(dryRunOption);
        Assert.NotNull(logLevelOption);
        Assert.NotNull(maxRetriesOption);
        Assert.NotNull(containerNamesOption);
        Assert.NotNull(partitionKeysOption);
        Assert.NotNull(configFileOption);
        Assert.NotNull(timeoutSecondsOption);
        Assert.NotNull(connectionStringOption);
        Assert.NotNull(outputFormatOption);

        Assert.Equal("--endpoint", endpointOption.Name);
        Assert.Equal("--database", databaseOption.Name);
        Assert.Equal("--dry-run", dryRunOption.Name);
        Assert.Equal("--log-level", logLevelOption.Name);
        Assert.Equal("--max-retries", maxRetriesOption.Name);
        Assert.Equal("--container-names", containerNamesOption.Name);
        Assert.Equal("--partition-keys", partitionKeysOption.Name);
        Assert.Equal("--config-file", configFileOption.Name);
        Assert.Equal("--timeout-seconds", timeoutSecondsOption.Name);
        Assert.Equal("--connection-string", connectionStringOption.Name);
        Assert.Equal("--output-format", outputFormatOption.Name);

        Assert.True(endpointOption.Required);
        Assert.False(databaseOption.Required);
        Assert.False(dryRunOption.Required);
        Assert.False(logLevelOption.Required);
        Assert.False(maxRetriesOption.Required);
        Assert.False(containerNamesOption.Required);
        Assert.False(partitionKeysOption.Required);
        Assert.False(configFileOption.Required);
        Assert.False(timeoutSecondsOption.Required);
        Assert.False(connectionStringOption.Required);
        Assert.False(outputFormatOption.Required);
    }
}