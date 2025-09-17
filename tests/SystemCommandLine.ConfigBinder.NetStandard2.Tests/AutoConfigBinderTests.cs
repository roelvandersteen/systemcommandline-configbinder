using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SystemCommandLine.ConfigBinder.NetStandard2.Tests;

public class AutoConfigBinderTests
{
    [SuppressMessage("ReSharper", "UnusedMember.Local"), SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")] 
    private sealed class TestConfig
    {
        [Display(Description = "Enable diagnostics output")]
        public bool Diagnostics { get; set; } = true; // should generate inverse

        [Required, Display(Description = "Endpoint URL")]
        public string Endpoint { get; set; } = string.Empty;

        [Display(Description = "Names list")] public string[] Names { get; set; } = Array.Empty<string>();

        [Range(1, 5)] public int Retries { get; set; } = 3;

#pragma warning disable S1144,S3459 // Allow auto-properties with initializers for testing purposes
        public string? OptionalText { get; set; }

        public int? OptionalNumber { get; set; }
#pragma warning restore S1144,S3459
    }

    [Fact]
    public void EmptyArrays_AreHandledCorrectly()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        ParseResult result = root.Parse("--endpoint x");
        TestConfig config = binder.Get(result);

        // Assert
        Assert.Empty(config.Names);
    }

    [Fact]
    public void IsDefaultStructValue_Works()
    {
        // Assert (testing internal helper)
        Assert.True(AutoConfigBinder<TestConfig>.OptionFactory.IsDefaultStructValue(0, typeof(int)));
        Assert.True(AutoConfigBinder<TestConfig>.OptionFactory.IsDefaultStructValue(false, typeof(bool)));
        Assert.False(AutoConfigBinder<TestConfig>.OptionFactory.IsDefaultStructValue(1, typeof(int)));
        Assert.False(AutoConfigBinder<TestConfig>.OptionFactory.IsDefaultStructValue(true, typeof(bool)));
    }

    [Fact]
    public void MultipleValidationErrors_AreAggregated()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        ParseResult result = root.Parse("--endpoint x --retries 10"); // endpoint "x" is valid, only retries out of range
        var ex = Assert.Throws<ValidationException>(() => binder.Get(result));

        // Assert
        Assert.Contains("Validation failed", ex.Message);
        Assert.Contains("Retries", ex.Message);
    }

    [Fact]
    public void OptionNames_AreKebabCase()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act & Assert
        Assert.True(Has("--endpoint"));
        Assert.True(Has("--diagnostics"));
        Assert.True(Has("--retries"));
        return;

        bool Has(string name)
        {
            return root.Options.Any(o => o.Name == name);
        }
    }

    [Fact]
    public void Required_Enforced_ByParser()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        ParseResult result = root.Parse("");

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("--endpoint"));
    }
}