namespace SystemCommandLine.ConfigBinder.Tests.Constants;

internal static class SourceConstants
{
    public const string SourceForBaseLine = """
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
}