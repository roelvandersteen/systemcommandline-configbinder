using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SystemCommandLine.ConfigBinder.Tests;

public class AutoConfigBinderTests
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    private sealed class TestConfig
    {
        [Required][Display(Description = "Endpoint URL")] public string Endpoint { get; set; } = string.Empty;

        [Display(Description = "Enable diagnostics output")] public bool Diagnostics { get; set; } = true; // should generate inverse

        [Range(1, 5)] public int Retries { get; set; } = 3;

        public string? OptionalText { get; set; }

        public int? OptionalNumber { get; set; }

        [Display(Description = "Names list")] public string[] Names { get; set; } = [];
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

        bool Has(string name) => root.Options.Any(o => o.Name == name);
    }

    [Fact]
    public void InverseBooleanOption_IsGenerated_WhenDefaultTrue()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act & Assert
        Assert.Contains(root.Options, o => o.Name == "--no-diagnostics");
    }

    private static readonly string[] expected = ["a", "b"];

    [Fact]
    public void Binding_Works_ForBasicValues()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint https://e/ --retries 4 --names a --names b");
        var config = binder.Get(result);

        // Assert
        Assert.Equal("https://e/", config.Endpoint);
        Assert.Equal(4, config.Retries);
        Assert.Equal(expected, config.Names);
        Assert.IsType<bool>(config.Diagnostics);
    }

    [Fact]
    public void InverseBooleanOption_OverridesPrimary()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x --no-diagnostics");
        var config = binder.Get(result);

        // Assert
        Assert.False(config.Diagnostics);
    }

    [Fact]
    public void Validation_Fails_ForOutOfRange()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x --retries 10");
        var ex = Assert.Throws<ValidationException>(() => binder.Get(result));

        // Assert
        Assert.Contains("Validation failed", ex.Message);
    }

    [Fact]
    public void Required_Enforced_ByParser()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("");

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("--endpoint"));
    }

    [Fact]
    public void DefaultValue_NotApplied_ForTrivialString()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var parse = root.Parse("");

        // Assert
        Assert.Contains(parse.Errors, e => e.Message.Contains("--endpoint"));
    }

    [Fact]
    public void DefaultValue_Applied_ForNonTrivial()
    {
        // Arrange (Retries has default 3 non-trivial)
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x");
        var config = binder.Get(result);

        // Assert
        Assert.Equal(3, config.Retries);
    }

    [Fact]
    public void Helper_IsTrivialReferenceDefault_Works()
    {
        // Assert (pure function tests)
        Assert.True(AutoConfigBinder<TestConfig>.IsTrivialReferenceDefault(""));
        Assert.True(AutoConfigBinder<TestConfig>.IsTrivialReferenceDefault(Array.Empty<int>()));
        Assert.False(AutoConfigBinder<TestConfig>.IsTrivialReferenceDefault("content"));
    }

    [Fact]
    public void GetOptionName_KebabCases()
    {
        // Assert (pure function)
        Assert.Equal("--some-property", AutoConfigBinder<TestConfig>.GetOptionName("SomeProperty"));
    }
}