using System.CommandLine;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SystemCommandLine.ConfigBinder.Tests;

public class GenerationSnapshotTests
{
    private const string Source = """
                                  using SystemCommandLine.ConfigBinder;

                                  namespace TestGeneration
                                  {
                                      public class AppConfig
                                      {
                                          public string Endpoint { get; set; } = string.Empty;
                                          public bool Diagnostics { get; set; } = true;
                                          public int Retries { get; set; } = 3;
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

        // Collect a broad set of references (core + current domain + CLI + attribute assembly) to ensure semantic model resolution.
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommandLineOptionsForAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RootCommand).Assembly.Location)
        };

        return CSharpCompilation.Create("Tests.Gen", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
}