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
        Type demoType = typeof(Demo.AppConfig);

        // Act & Assert - Both should use Required and Display attributes
        PropertyInfo? codeGenEndpoint = codeGenType.GetProperty("Endpoint");
        PropertyInfo? demoEndpoint = demoType.GetProperty("Endpoint");

        Assert.NotNull(codeGenEndpoint);
        Assert.NotNull(demoEndpoint);

        // Both should have Required attribute on Endpoint
        Assert.True(codeGenEndpoint.GetCustomAttributes(typeof(RequiredAttribute), false).Any());
        Assert.True(demoEndpoint.GetCustomAttributes(typeof(RequiredAttribute), false).Any());

        // Both should have Display attributes
        Assert.True(codeGenEndpoint.GetCustomAttributes(typeof(DisplayAttribute), false).Any());
        Assert.True(demoEndpoint.GetCustomAttributes(typeof(DisplayAttribute), false).Any());
    }

    [Fact]
    public void CodeGenerationSample_AppConfig_ShouldMatchDemoAppConfig_Structure()
    {
        // Arrange
        var codeGenConfig = new AppConfig();
        var demoConfig = new Demo.AppConfig();

        // Act & Assert - Both should have Endpoint, Diagnostics/DryRun, and Retries/MaxRetries
        Assert.NotNull(codeGenConfig.Endpoint);
        Assert.NotNull(demoConfig.Endpoint);

        // Both have retry-related properties
        Assert.True(codeGenConfig.Retries > 0);
        Assert.True(demoConfig.MaxRetries > 0);

        // Both have boolean flags
        Assert.IsType<bool>(codeGenConfig.Diagnostics);
        Assert.IsType<bool>(demoConfig.DryRun);
    }
}