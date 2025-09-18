using System.CommandLine;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SystemCommandLine.ConfigBinder.Generators;

namespace SystemCommandLine.ConfigBinder.Tests.Helpers;

internal static class GeneratorTestHelper
{
    private const string PropertyName = "TestProperty";
    private const string OptionName = $"{PropertyName}Option";

    public static void AssertExpectedDefaultFactory(bool shouldHaveFactory, string? expectedValue, string generatedSourceText, string comment)
    {
        var optionSection = GetOptionSection(generatedSourceText);
        string? message;
        if (shouldHaveFactory)
        {
            var expectedFactoryText = $"DefaultValueFactory = _ => {expectedValue ?? "MISSING_EXPECTED_VALUE"}";

            Assert.Contains(expectedFactoryText, optionSection);

            message = !optionSection.Contains(expectedFactoryText) ? $"Expected DefaultValueFactory incorrect for {comment}" : null;
        }
        else
        {
            Assert.DoesNotContain("DefaultValueFactory", optionSection);

            message = optionSection.Contains("DefaultValueFactory") ? $"DefaultValueFactory not expected for {comment}" : null;
        }

        // Print message that helps distinguish between the many test cases
        if (!string.IsNullOrEmpty(message))
        {
            Assert.Fail(message);
        }
    }

    public static string GenerateCodeForProperty(string propertyTypeString, string defaultValueString, bool enableNullable = false)
    {
        var nullableDirective = enableNullable ? "#nullable enable" : "";
        return $$"""
                 {{nullableDirective}}
                 using SystemCommandLine.ConfigBinder;
                 using TestGeneration;

                 namespace TestGeneration
                 {
                     public class AppConfig
                     {
                         public {{propertyTypeString}} {{PropertyName}} { get; set; } = {{defaultValueString}};
                     }
                 }

                 [CommandLineOptionsFor(typeof(AppConfig))]
                 public partial class AppConfigOptions { }
                 """;
    }

    public static GeneratorResult RunGeneratorAndGetResult(GeneratorDriver driver, CSharpCompilation compilation)
    {
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var diagnosticsText = "Generator diagnostics: " + string.Join("\n", diagnostics.Select(d => d.ToString()));
            return new GeneratorResult(string.Empty, true, diagnosticsText);
        }

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        GeneratedSourceResult generatedSourceResult = runResult.Results.SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "AppConfigOptions.CommandLineOptions.g.cs");

        return new GeneratorResult(generatedSourceResult.SourceText?.ToString() ?? string.Empty, false, string.Empty);
    }

    public static CSharpCompilation SetupCSharpCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        return CSharpCompilation.Create("Tests.Gen",
            [syntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static string GetOptionSection(string generated)
    {
        var startIndex = generated.IndexOf($"{OptionName} {{", StringComparison.Ordinal);
        if (startIndex == -1)
        {
            Assert.Fail($"Option '{OptionName}' not found in generated code");
        }

        var endIndex = generated.IndexOf("};", startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
        {
            Assert.Fail($"End of option section not found for '{OptionName}'");
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

    public record GeneratorResult(string SourceText, bool HasErrorDiagnostics, string DiagnosticsText);

    public static class TestContext
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
}