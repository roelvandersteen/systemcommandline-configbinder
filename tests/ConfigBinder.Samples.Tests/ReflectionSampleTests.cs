using ConfigBinder.Reflection;
using Serilog;
using Serilog.Core;

namespace ConfigBinder.Samples.Tests;

public class ReflectionSampleTests
{
    [Theory, InlineData("https://example.com", "TestDb", true, LogLevel.Debug, 5),
     InlineData("https://test.com", "ProdDb", false, LogLevel.Warning, 1)]
    public async Task AppProcessor_ProcessAsync_WithVariousConfigs_ShouldWork(string endpoint, string database, bool dryRun, LogLevel logLevel,
        int maxRetries)
    {
        // Arrange
        Logger logger = new LoggerConfiguration().CreateLogger();
        var processor = new AppProcessor(logger);
        var config = new AppConfig
        {
            Endpoint = endpoint,
            Database = database,
            DryRun = dryRun,
            LogLevel = logLevel,
            MaxRetries = maxRetries
        };

        // Act
        var result = await processor.ProcessAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void AppConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.Equal(string.Empty, config.Endpoint);
        Assert.Equal("SubscriptionsDb", config.Database);
        Assert.True(config.DryRun);
        Assert.Equal(LogLevel.Information, config.LogLevel);
        Assert.Equal(3, config.MaxRetries);
    }

    [Fact]
    public async Task AppProcessor_ProcessAsync_ShouldReturnZero()
    {
        // Arrange
        Logger logger = new LoggerConfiguration().CreateLogger();
        var processor = new AppProcessor(logger);
        var config = new AppConfig
        {
            Endpoint = "https://example.com",
            Database = "TestDb",
            DryRun = true,
            LogLevel = LogLevel.Information,
            MaxRetries = 3
        };

        // Act
        var result = await processor.ProcessAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void AppProcessor_ShouldNotBeNull()
    {
        // Arrange
        Logger logger = new LoggerConfiguration().CreateLogger();

        // Act
        var processor = new AppProcessor(logger);

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<IAppProcessor>(processor);
    }

    [Fact]
    public async Task Program_ExitCode_ShouldBeZero()
    {
        // Arrange & Act
        var exitCode = await Program.Main(["--endpoint", "https://example.com"]);

        // Assert
        Assert.Equal(0, exitCode);
    }
}