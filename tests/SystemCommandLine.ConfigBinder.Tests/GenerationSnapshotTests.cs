using System.CommandLine;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SystemCommandLine.ConfigBinder.Generators;

namespace SystemCommandLine.ConfigBinder.Tests;

public class GenerationSnapshotTests
{
    private const string Source = """
                                  using System.ComponentModel.DataAnnotations;
                                  using SystemCommandLine.ConfigBinder;

                                  namespace TestGeneration
                                  {
                                      public class AppConfig
                                      {
                                          [Required] public string Endpoint { get; set; } = string.Empty;
                                          public bool Diagnostics { get; set; } = true;
                                          public int Retries { get; set; } = 3;
                                      }
                                  
                                      [CommandLineOptionsFor(typeof(AppConfig))]
                                      public partial class AppConfigOptions { }
                                  }
                                  """;

    private const string SourceWithTrivialDefaults = """
                                                     using System.ComponentModel.DataAnnotations;
                                                     using SystemCommandLine.ConfigBinder;

                                                     namespace TestGeneration
                                                     {
                                                         public class AppConfig
                                                         {
                                                             public bool Verbose { get; set; } = false;     // Should NOT get DefaultValueFactory (trivial default)
                                                             public int Count { get; set; } = 0;            // Should NOT get DefaultValueFactory (trivial default)
                                                             public long LongValue { get; set; } = 0L;      // Should NOT get DefaultValueFactory (trivial default)
                                                             public decimal Price { get; set; } = 0m;       // Should NOT get DefaultValueFactory (trivial default)
                                                             public float FloatValue { get; set; } = 0f;    // Should NOT get DefaultValueFactory (trivial default)
                                                             public double DoubleValue { get; set; } = 0d;  // Should NOT get DefaultValueFactory (trivial default)
                                                             public uint UIntValue { get; set; } = 0U;      // Should NOT get DefaultValueFactory (trivial default)
                                                             public ulong ULongValue { get; set; } = 0UL;   // Should NOT get DefaultValueFactory (trivial default)
                                                             public char NullChar { get; set; } = '\0';     // Should NOT get DefaultValueFactory (trivial default)
                                                             public bool Enabled { get; set; } = true;      // Should get DefaultValueFactory (meaningful default)
                                                             public int MaxItems { get; set; } = 100;       // Should get DefaultValueFactory (meaningful default)
                                                             public decimal Tax { get; set; } = 0.15m;      // Should get DefaultValueFactory (meaningful default)
                                                         }
                                                     
                                                         [CommandLineOptionsFor(typeof(AppConfig))]
                                                         public partial class AppConfigOptions { }
                                                     }
                                                     """;

    private static Assembly LoadGeneratorAssembly()
    {
        const string assemblyName = "SystemCommandLine.ConfigBinder.Generators";
        const string assemblyFileName = $"{assemblyName}.dll";
        Assembly? loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        var projectRoot = TestContext.ProjectRoot;
        var binPath = Path.Combine(projectRoot, "..", "..", "src", assemblyName, "bin");
        var possiblePaths = new[]
        {
            Path.GetFullPath(Path.Combine(binPath, "Debug", "netstandard2.0", assemblyFileName)),
            Path.GetFullPath(Path.Combine(binPath, "Release", "netstandard2.0", assemblyFileName)),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, assemblyFileName)
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Assembly.Load(Path.GetFileNameWithoutExtension(path));
            }
        }

        var searchedPaths = string.Join("\n  ", possiblePaths);
        Assert.Fail($"Generator assembly not found. Searched:\n  {searchedPaths}");
        throw new InvalidOperationException("Generator assembly not found.");
    }

    internal static class TestContext
    {
        static TestContext()
        {
            var directory = Directory.GetCurrentDirectory();
            while (directory is not null && !File.Exists(Path.Combine(directory, "SystemCommandLine.ConfigBinder.Tests.csproj")))
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            ProjectRoot = directory ?? throw new DirectoryNotFoundException("Could not locate test project root.");
        }

        public static string ProjectRoot { get; }
    }

    private static CSharpCompilation SetupCSharpCompilation()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(Source, parseOptions);

        return CSharpCompilation.Create("Tests.Gen",
            [syntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Theory, InlineData("Hello World", "Hello World"), InlineData("Hello \"World\"", "Hello \\\"World\\\""),
     InlineData("Hello\\nWorld", "Hello\\\\nWorld"), InlineData("Hello\nWorld", "Hello\\nWorld"), InlineData("Hello\rWorld", "Hello\\rWorld"),
     InlineData("Hello\\\"World", "Hello\\\\\\\"World"), InlineData("Path\\to\\file\nLine 2", "Path\\\\to\\\\file\\nLine 2")]
    public void EscapeString_HandlesSpecialCharacters(string input, string expected)
    {
        // Use reflection to call the private EscapeString method
        Type generatorType = typeof(CommandLineOptionsGenerator);
        MethodInfo? escapeMethod = generatorType.GetMethod("EscapeString", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(escapeMethod);
        var result = (string)escapeMethod.Invoke(null, [input])!;

        Assert.Equal(expected, result);
    }

    private static void AssertNoDefaultFactory(string generated, string optionName)
    {
        var optionSection = GetOptionSection(generated, optionName);
        Assert.DoesNotContain("DefaultValueFactory", optionSection);
    }

    private static void AssertHasDefaultFactory(string generated, string optionName, string expectedValue)
    {
        var optionSection = GetOptionSection(generated, optionName);
        Assert.Contains($"DefaultValueFactory = _ => {expectedValue}", optionSection);
    }

    private static string GetOptionSection(string generated, string optionName)
    {
        var startIndex = generated.IndexOf($"{optionName} {{", StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return "";
        }

        var endIndex = generated.IndexOf("};", startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
        {
            return "";
        }

        return generated.Substring(startIndex, endIndex - startIndex + 2);
    }

    private static MetadataReference[] GetReferences()
    {
        return
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommandLineOptionsForAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RootCommand).Assembly.Location)
        ];
    }

    [Fact]
    public void GeneratedSource_MatchesBaseline()
    {
        // Arrange
        CSharpCompilation compilation = SetupCSharpCompilation();

        Assembly generatorAssembly = LoadGeneratorAssembly();
        Type generatorType = generatorAssembly.GetType("SystemCommandLine.ConfigBinder.Generators.CommandLineOptionsGenerator", true)!;
        var generator = (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? _, out var diagnostics);

        Assert.True(!diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
            "Generator diagnostics: " + string.Join("\n", diagnostics.Select(d => d.ToString())));

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        GeneratedSourceResult generatedSourceResult = runResult.Results.SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "AppConfigOptions.CommandLineOptions.g.cs");

        var generated = generatedSourceResult.SourceText?.ToString() ?? string.Empty;

        generated = Normalize(generated);

        var baselinePath = Path.Combine(TestContext.ProjectRoot, "Baselines", "AppConfigOptions.CommandLineOptions.g.txt");
        var baseline = Normalize(File.ReadAllText(baselinePath));

        // Assert
        Assert.Equal(baseline, generated);

        if (baseline == generated)
        {
            return;
        }

        // Emit diff-friendly message
        var sb = new StringBuilder();
        sb.AppendLine("Generated source did not match baseline.");
        sb.AppendLine("--- Generated ---");
        sb.AppendLine(generated);
        sb.AppendLine("--- Baseline ---");
        sb.AppendLine(baseline);
        Assert.Fail(sb.ToString());

        return;

        // Normalize line endings and trim whitespace lines
        static string Normalize(string s)
        {
            return string.Join("\n", s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Select(l => l.TrimEnd()));
        }
    }

    [Fact]
    public void TrivialDefaults_ShouldNotHaveDefaultValueFactory()
    {
        // Arrange
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceWithTrivialDefaults);
        var compilation = CSharpCompilation.Create("Tests.Gen",
            [syntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assembly generatorAssembly = LoadGeneratorAssembly();
        Type generatorType = generatorAssembly.GetType("SystemCommandLine.ConfigBinder.Generators.CommandLineOptionsGenerator", true)!;
        var generator = (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        Assert.True(!diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
            "Generator diagnostics: " + string.Join("\n", diagnostics.Select(d => d.ToString())));

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        GeneratedSourceResult generatedSourceResult = runResult.Results.SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "AppConfigOptions.CommandLineOptions.g.cs");

        var generated = generatedSourceResult.SourceText?.ToString() ?? string.Empty;

        // Assert - Properties with trivial defaults should NOT have DefaultValueFactory
        AssertNoDefaultFactory(generated, "VerboseOption"); // bool = false
        AssertNoDefaultFactory(generated, "CountOption"); // int = 0
        AssertNoDefaultFactory(generated, "LongValueOption"); // long = 0L
        AssertNoDefaultFactory(generated, "PriceOption"); // decimal = 0m
        AssertNoDefaultFactory(generated, "FloatValueOption"); // float = 0f
        AssertNoDefaultFactory(generated, "DoubleValueOption"); // double = 0d
        AssertNoDefaultFactory(generated, "UIntValueOption"); // uint = 0U
        AssertNoDefaultFactory(generated, "ULongValueOption"); // ulong = 0UL
        AssertNoDefaultFactory(generated, "NullCharOption"); // char = '\0'

        // Properties with meaningful defaults should HAVE DefaultValueFactory
        AssertHasDefaultFactory(generated, "EnabledOption", "true"); // bool = true
        AssertHasDefaultFactory(generated, "MaxItemsOption", "100"); // int = 100
        AssertHasDefaultFactory(generated, "TaxOption", "0.15m"); // decimal = 0.15m
    }
}