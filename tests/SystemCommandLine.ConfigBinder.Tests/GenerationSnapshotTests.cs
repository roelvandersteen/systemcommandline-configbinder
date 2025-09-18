using System.Collections;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SystemCommandLine.ConfigBinder.Generators;
using SystemCommandLine.ConfigBinder.Tests.Constants;
using SystemCommandLine.ConfigBinder.Tests.Helpers;

namespace SystemCommandLine.ConfigBinder.Tests;

public class GenerationSnapshotTests
{
    public class DefaultValueTestData : IEnumerable<object[]>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            // Trivial defaults
            yield return ["bool", "false", false, null!, "Trivial boolean default"];
            yield return ["bool", "true", true, "true", "Meaningful boolean default"];
            yield return ["int", "0", false, null!, "Trivial int default"];
            yield return ["int", "100", true, "100", "Meaningful int default"];
            yield return ["long", "0L", false, null!, "Trivial long default"];
            yield return ["decimal", "0m", false, null!, "Trivial decimal default"];
            yield return ["decimal", "0.15m", true, "0.15m", "Meaningful decimal default"];
            yield return ["float", "0f", false, null!, "Trivial float default"];
            yield return ["double", "0d", false, null!, "Trivial double default"];
            yield return ["uint", "0U", false, null!, "Trivial uint default"];
            yield return ["ulong", "0UL", false, null!, "Trivial ulong default"];
            yield return ["char", "'\\0'", false, null!, "Trivial char default"];

            // Nullable types
            yield return ["int?", "null", false, null!, "Nullable int with null default"];
            yield return ["string?", "null", false, null!, "Nullable string with null default"];
            yield return ["bool?", "null", false, null!, "Nullable bool with null default"];
            yield return ["int?", "5", true, "5", "Nullable int with meaningful non-null default"];
            yield return ["string?", "\"default\"", true, "\"default\"", "Nullable string with meaningful non-null default"];
            yield return ["bool?", "true", true, "true", "Nullable bool with meaningful non-null default"];
            yield return ["int?", "0", false, null!, "Nullable int with trivial non-null default"];
            yield return ["bool?", "false", false, null!, "Nullable bool with trivial non-null default"];
            yield return ["string?", "\"\"", false, null!, "Nullable string with empty default"];

            // Arrays
            yield return ["string[]?", "null", false, null!, "Null string array"];
            yield return ["int[]?", "null", false, null!, "Null int array"];
            yield return ["string[]", "[]", false, null!, "Empty array (collection expression)"];
            yield return ["int[]", "new int[0]", false, null!, "Empty array (sized)"];
            yield return ["string[]", "new string[] { }", false, null!, "Empty array (classic syntax)"];
            yield return ["string[]", "[\"default\", \"values\"]", true, "[\"default\", \"values\"]", "Meaningful string array"];
            yield return ["int[]", "[1, 2, 3]", true, "[1, 2, 3]", "Meaningful int array"];
            yield return ["string[]", "[\"single\"]", true, "[\"single\"]", "Single value array"];
            yield return
            [
                "string[]", "new string[] { \"classic\", \"syntax\" }", true, "new string[] { \"classic\", \"syntax\" }",
                "Classic string array syntax"
            ];
            yield return ["int[]", "new int[] { 42, 99 }", true, "new int[] { 42, 99 }", "Classic int array syntax"];
        }
    }

    [Theory, ClassData(typeof(DefaultValueTestData))]
    public void DefaultValues_ShouldHandleCorrectly_Theory(string propertyType, string defaultValue, bool shouldHaveFactory, string? expectedValue,
        string comment = "")
    {
        // Arrange
        var source = GeneratorTestHelper.GenerateCodeForProperty(propertyType, defaultValue);
        CSharpCompilation compilation = GeneratorTestHelper.SetupCSharpCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CommandLineOptionsGenerator());

        // Act
        GeneratorTestHelper.GeneratorResult generated = GeneratorTestHelper.RunGeneratorAndGetResult(driver, compilation);

        // Assert
        Assert.False(generated.HasErrorDiagnostics, generated.DiagnosticsText);
        GeneratorTestHelper.AssertExpectedDefaultFactory(shouldHaveFactory, expectedValue, generated.SourceText, comment);
    }

    [Fact]
    public void GeneratedSource_MatchesBaseline()
    {
        // Arrange
        CSharpCompilation compilation = GeneratorTestHelper.SetupCSharpCompilation(SourceConstants.SourceForBaseLine);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CommandLineOptionsGenerator());

        var baselinePath = Path.Combine(GeneratorTestHelper.TestContext.ProjectRoot, "Baselines", "AppConfigOptions.CommandLineOptions.g.txt");
        var baseline = Normalize(File.ReadAllText(baselinePath));

        // Act
        GeneratorTestHelper.GeneratorResult result = GeneratorTestHelper.RunGeneratorAndGetResult(driver, compilation);
        var generatedSourceText = Normalize(result.SourceText);

        // Assert
        Assert.False(result.HasErrorDiagnostics, result.DiagnosticsText);
        Assert.Equal(baseline, generatedSourceText);

        if (baseline == generatedSourceText)
        {
            return;
        }

        // Emit diff-friendly message
        var sb = new StringBuilder();
        sb.AppendLine("Generated source did not match baseline.");
        sb.AppendLine("--- Generated ---");
        sb.AppendLine(generatedSourceText);
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