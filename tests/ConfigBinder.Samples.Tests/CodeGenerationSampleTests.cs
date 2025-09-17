using System.CommandLine;
using ConfigBinder.CodeGeneration;

namespace ConfigBinder.Samples.Tests;

public class CodeGenerationSampleTests
{
    [Fact]
    public void AppConfigOptions_ShouldHaveCorrectOptions()
    {
        // Arrange & Act - Test that the generated options exist and have correct types
        var endpointOption = AppConfigOptions.EndpointOption;
        var diagnosticsOption = AppConfigOptions.DiagnosticsOption;
        var retriesOption = AppConfigOptions.RetriesOption;

        // Assert
        Assert.NotNull(endpointOption);
        Assert.NotNull(diagnosticsOption);
        Assert.NotNull(retriesOption);

        Assert.Equal("--endpoint", endpointOption.Name);
        Assert.Equal("--diagnostics", diagnosticsOption.Name);
        Assert.Equal("--retries", retriesOption.Name);

        Assert.True(endpointOption.Required);
        Assert.False(diagnosticsOption.Required);
        Assert.False(retriesOption.Required);
    }

    [Fact]
    public void AppConfigOptions_AddOptionsTo_ShouldAddAllOptions()
    {
        // Arrange
        var command = new RootCommand();

        // Act
        AppConfigOptions.AddOptionsTo(command);

        // Assert - The command will include both positive and negative options for booleans
        Assert.True(command.Options.Count >= 3); // At least the basic 3 options
        Assert.Contains(command.Options, o => o.Name == "--endpoint");
        Assert.Contains(command.Options, o => o.Name == "--diagnostics");
        Assert.Contains(command.Options, o => o.Name == "--retries");
    }

    [Fact]
    public void AppConfigOptions_Get_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://example.com", "--retries", "5" };

        // Act
        var parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://example.com", config.Endpoint);
        Assert.True(config.Diagnostics); // Default value
        Assert.Equal(5, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithNoDiagnostics_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://test.com", "--diagnostics", "false", "--retries", "1" };

        // Act
        var parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://test.com", config.Endpoint);
        Assert.False(config.Diagnostics);
        Assert.Equal(1, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithInvalidRetries_ShouldNotThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://example.com", "--retries", "15" }; // Out of range
        var parseResult = command.Parse(args);

        // Act - The validation happens at the model level, not during parsing
        var config = AppConfigOptions.Get(parseResult);

        // Assert - Value is set even if out of range, validation would happen elsewhere
        Assert.NotNull(config);
        Assert.Equal(15, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithMissingRequiredEndpoint_ShouldThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--retries", "5" }; // Missing required endpoint
        var parseResult = command.Parse(args);

        // Act & Assert - Should throw InvalidOperationException for missing required option
        Assert.Throws<InvalidOperationException>(() => AppConfigOptions.Get(parseResult));
    }
}

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
        Assert.True(command.Options.Count >= 3); // At least the basic 3 options
        Assert.Contains(command.Options, o => o.Name == "--endpoint");
        Assert.Contains(command.Options, o => o.Name == "--diagnostics");
        Assert.Contains(command.Options, o => o.Name == "--retries");
    }

    [Fact]
    public void AppConfigOptions_Get_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://example.com", "--retries", "5" };

        // Act
        ParseResult parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://example.com", config.Endpoint);
        Assert.True(config.Diagnostics); // Default value
        Assert.Equal(5, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithInvalidRetries_ShouldNotThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--endpoint", "https://example.com", "--retries", "15" }; // Out of range
        ParseResult parseResult = command.Parse(args);

        // Act - The validation happens at the model level, not during parsing
        var config = AppConfigOptions.Get(parseResult);

        // Assert - Value is set even if out of range, validation would happen elsewhere
        Assert.NotNull(config);
        Assert.Equal(15, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_Get_WithMissingRequiredEndpoint_ShouldThrow()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[] { "--retries", "5" }; // Missing required endpoint
        ParseResult parseResult = command.Parse(args);

        // Act & Assert - Should throw InvalidOperationException for missing required option
        Assert.Throws<InvalidOperationException>(() => AppConfigOptions.Get(parseResult));
    }

    [Fact]
    public void AppConfigOptions_Get_WithNoDiagnostics_ShouldBindCorrectly()
    {
        // Arrange
        var command = new RootCommand();
        AppConfigOptions.AddOptionsTo(command);
        var args = new[]
        {
            "--endpoint",
            "https://test.com",
            "--diagnostics",
            "false",
            "--retries",
            "1"
        };

        // Act
        ParseResult parseResult = command.Parse(args);
        var config = AppConfigOptions.Get(parseResult);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://test.com", config.Endpoint);
        Assert.False(config.Diagnostics);
        Assert.Equal(1, config.Retries);
    }

    [Fact]
    public void AppConfigOptions_ShouldHaveCorrectOptions()
    {
        // Arrange & Act - Test that the generated options exist and have correct types
        var endpointOption = AppConfigOptions.EndpointOption;
        var diagnosticsOption = AppConfigOptions.DiagnosticsOption;
        var retriesOption = AppConfigOptions.RetriesOption;

        // Assert
        Assert.NotNull(endpointOption);
        Assert.NotNull(diagnosticsOption);
        Assert.NotNull(retriesOption);

        Assert.Equal("--endpoint", endpointOption.Name);
        Assert.Equal("--diagnostics", diagnosticsOption.Name);
        Assert.Equal("--retries", retriesOption.Name);

        Assert.True(endpointOption.Required);
        Assert.False(diagnosticsOption.Required);
        Assert.False(retriesOption.Required);
    }
}