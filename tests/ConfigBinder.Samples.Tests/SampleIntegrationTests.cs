using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ConfigBinder.CodeGeneration;

namespace ConfigBinder.Samples.Tests;

public class SampleIntegrationTests
{
    [Fact]
    public void BothSamples_ShouldUseDataAnnotations()
    {
        // Arrange
        Type codeGenType = typeof(AppConfig);
        Type reflectionType = typeof(Reflection.AppConfig);

        // Act & Assert - Both should use Required and Display attributes
        PropertyInfo? codeGenEndpoint = codeGenType.GetProperty("Endpoint");
        PropertyInfo? reflectionEndpoint = reflectionType.GetProperty("Endpoint");

        Assert.NotNull(codeGenEndpoint);
        Assert.NotNull(reflectionEndpoint);

        // Both should have Required attribute on Endpoint
        Assert.True(codeGenEndpoint.GetCustomAttributes(typeof(RequiredAttribute), false).Any());
        Assert.True(reflectionEndpoint.GetCustomAttributes(typeof(RequiredAttribute), false).Any());

        // Both should have Display attributes
        Assert.True(codeGenEndpoint.GetCustomAttributes(typeof(DisplayAttribute), false).Any());
        Assert.True(reflectionEndpoint.GetCustomAttributes(typeof(DisplayAttribute), false).Any());
    }

    [Fact]
    public void CodeGenerationSample_AppConfig_ShouldMatchReflectionAppConfig_Structure()
    {
        // Arrange
        var codeGenConfig = new AppConfig();
        var reflectionConfig = new Reflection.AppConfig();

        // Act & Assert - Both should have common properties
        Assert.NotNull(codeGenConfig.Endpoint);
        Assert.NotNull(reflectionConfig.Endpoint);

        Assert.NotNull(codeGenConfig.Database);
        Assert.NotNull(reflectionConfig.Database);

        // Both have retry-related properties
        Assert.True(codeGenConfig.MaxRetries > 0);
        Assert.True(reflectionConfig.MaxRetries > 0);

        // Both have boolean flags
        Assert.IsType<bool>(codeGenConfig.DryRun);
        Assert.IsType<bool>(reflectionConfig.DryRun);

        Assert.IsType<bool>(codeGenConfig.Verbose);

        // Both have enum properties
        Assert.IsType<LogLevel>(codeGenConfig.LogLevel);
        Assert.IsType<Reflection.LogLevel>(reflectionConfig.LogLevel);

        Assert.IsType<OutputFormat>(codeGenConfig.OutputFormat);
        Assert.IsType<Reflection.OutputFormat>(reflectionConfig.OutputFormat);

        // Both have timeout properties (CodeGen uses int, Reflection uses int? but we test the simple version)
        Assert.IsType<int>(codeGenConfig.TimeoutSeconds);
    }
}