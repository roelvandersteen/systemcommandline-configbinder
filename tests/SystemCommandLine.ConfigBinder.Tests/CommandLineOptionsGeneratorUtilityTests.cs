using SystemCommandLine.ConfigBinder.Generators;

namespace SystemCommandLine.ConfigBinder.Tests;

public class CommandLineOptionsGeneratorUtilityTests
{
    [Theory, InlineData("Hello World", "Hello World"), InlineData("Hello \"World\"", "Hello \\\"World\\\""),
     InlineData("Hello\\nWorld", "Hello\\\\nWorld"), InlineData("Hello\nWorld", "Hello\\nWorld"), InlineData("Hello\rWorld", "Hello\\rWorld"),
     InlineData("Hello\\\"World", "Hello\\\\\\\"World"), InlineData("Path\\to\\file\nLine 2", "Path\\\\to\\\\file\\nLine 2")]
    public void EscapeString_HandlesSpecialCharacters(string input, string expected)
    {
        // Arrange & Act
        var result = CommandLineOptionsGenerator.EscapeString(input);

        // Assert
        Assert.Equal(expected, result);
    }
}