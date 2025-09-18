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

        // Act & Assert - Both should have Endpoint, Diagnostics/DryRun, and Retries/MaxRetries
        Assert.NotNull(codeGenConfig.Endpoint);
        Assert.NotNull(reflectionConfig.Endpoint);

        // Both have retry-related properties
        Assert.True(codeGenConfig.Retries > 0);
        Assert.True(reflectionConfig.MaxRetries > 0);

        // Both have boolean flags
        Assert.IsType<bool>(codeGenConfig.Diagnostics);
        Assert.IsType<bool>(reflectionConfig.DryRun);
    }
}