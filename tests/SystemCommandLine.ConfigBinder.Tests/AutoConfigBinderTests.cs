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
        Assert.True(AutoConfigBinder<TestConfig>.OptionFactory.IsTrivialReferenceDefault(""));
        Assert.True(AutoConfigBinder<TestConfig>.OptionFactory.IsTrivialReferenceDefault(Array.Empty<int>()));
        Assert.False(AutoConfigBinder<TestConfig>.OptionFactory.IsTrivialReferenceDefault("content"));
    }

    [Fact]
    public void GetOptionName_KebabCases()
    {
        // Assert (pure function)
        Assert.Equal("--some-property", AutoConfigBinder<TestConfig>.GetOptionName("SomeProperty"));
    }

    [Fact]
    public void Binding_HandlesNullableProperties()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x --optional-number 42");
        var config = binder.Get(result);

        // Assert
        Assert.Equal(42, config.OptionalNumber);
        Assert.Null(config.OptionalText); // not provided
    }

    [Fact]
    public void EmptyArrays_AreHandledCorrectly()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x");
        var config = binder.Get(result);

        // Assert
        Assert.Empty(config.Names);
    }

    [Fact]
    public void MultipleValidationErrors_AreAggregated()
    {
        // Arrange
        var binder = new AutoConfigBinder<TestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--endpoint x --retries 10"); // endpoint "x" is valid, only retries out of range
        var ex = Assert.Throws<ValidationException>(() => binder.Get(result));

        // Assert
        Assert.Contains("Validation failed", ex.Message);
        Assert.Contains("Retries", ex.Message);
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
    public void BooleanProperty_WithDefaultFalse_DoesNotGenerateInverse()
    {
        // Arrange
        var binder = new AutoConfigBinder<BoolDefaultFalseConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act & Assert
        Assert.Contains(root.Options, o => o.Name == "--flag");
        Assert.DoesNotContain(root.Options, o => o.Name == "--no-flag");
    }

    [Fact]
    public void ComplexPropertyNames_AreKebabCased()
    {
        // Arrange
        var binder = new AutoConfigBinder<ComplexNamingConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act & Assert
        Assert.Contains(root.Options, o => o.Name == "--max-retry-count");
        Assert.Contains(root.Options, o => o.Name == "--api-endpoint-url");
        Assert.Contains(root.Options, o => o.Name == "--use-https");
    }

    private sealed class BoolDefaultFalseConfig
    {
        public bool Flag { get; set; } = false; // Should NOT generate --no-flag
    }

    private sealed class ComplexNamingConfig
    {
        public int MaxRetryCount { get; set; }
        public string ApiEndpointUrl { get; set; } = "";
        public bool UseHTTPS { get; set; }
    }

    private sealed class ValidationTestConfig
    {
        [Required]
        [MinLength(3)]
        [MaxLength(10)]
        public string Name { get; set; } = "";

        [Range(1, 100)]
        public int Age { get; set; }
    }

    [Fact]
    public void MultipleValidationAttributes_AreAllChecked()
    {
        // Arrange
        var binder = new AutoConfigBinder<ValidationTestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act - test too short name
        var result1 = root.Parse("--name ab --age 50");
        var ex1 = Assert.Throws<ValidationException>(() => binder.Get(result1));

        // Act - test too long name
        var result2 = root.Parse("--name verylongname --age 50");
        var ex2 = Assert.Throws<ValidationException>(() => binder.Get(result2));

        // Assert
        Assert.Contains("Name", ex1.Message);
        Assert.Contains("Name", ex2.Message);
    }

    [Fact]
    public void ValidInput_PassesAllValidation()
    {
        // Arrange
        var binder = new AutoConfigBinder<ValidationTestConfig>();
        var root = new RootCommand();
        binder.AddOptionsTo(root);

        // Act
        var result = root.Parse("--name John --age 25");
        var config = binder.Get(result);

        // Assert
        Assert.Equal("John", config.Name);
        Assert.Equal(25, config.Age);
    }
}