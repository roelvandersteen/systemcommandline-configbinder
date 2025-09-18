namespace SystemCommandLine.ConfigBinder.Tests.Constants;

internal static class SourceConstants
{
    public const string Source = """
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

    public const string SourceWithTrivialDefaults = """
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

    public const string SourceWithNullableDefaults = """
                                                     #nullable enable
                                                     using System.ComponentModel.DataAnnotations;
                                                     using SystemCommandLine.ConfigBinder;

                                                     namespace TestGeneration
                                                     {
                                                         public class AppConfig
                                                         {
                                                             // Non-nullable with trivial defaults - should NOT get DefaultValueFactory
                                                             public int Count { get; set; } = 0;
                                                             public bool Verbose { get; set; } = false;
                                                             
                                                             // Non-nullable with meaningful defaults - should get DefaultValueFactory
                                                             public int MaxRetries { get; set; } = 3;
                                                             public bool EnableLogging { get; set; } = true;
                                                             
                                                             // Nullable with null defaults - should NOT get DefaultValueFactory (trivial)
                                                             public int? OptionalCount { get; set; } = null;
                                                             public string? OptionalName { get; set; } = null;
                                                             public bool? OptionalFlag { get; set; } = null;
                                                             
                                                             // Nullable with non-null defaults - should get DefaultValueFactory (meaningful)
                                                             public int? DefaultCount { get; set; } = 5;
                                                             public string? DefaultName { get; set; } = "default";
                                                             public bool? DefaultFlag { get; set; } = true;
                                                             
                                                             // Nullable with trivial non-null defaults - should NOT get DefaultValueFactory
                                                             public int? ZeroCount { get; set; } = 0;
                                                             public bool? FalseFlag { get; set; } = false;
                                                             
                                                             // Nullable string with empty default - should NOT get DefaultValueFactory (trivial)
                                                             public string? EmptyString { get; set; } = "";
                                                         }
                                                     
                                                         [CommandLineOptionsFor(typeof(AppConfig))]
                                                         public partial class AppConfigOptions { }
                                                     }
                                                     """;

    public const string SourceWithArrayDefaults = """
                                                  using System.ComponentModel.DataAnnotations;
                                                  using SystemCommandLine.ConfigBinder;

                                                  namespace TestGeneration
                                                  {
                                                      public class AppConfig
                                                      {
                                                          // Arrays with null defaults - should NOT get DefaultValueFactory (trivial)
                                                          public string[]? NullStringArray { get; set; } = null;
                                                          public int[]? NullIntArray { get; set; } = null;
                                                          
                                                          // Arrays with empty defaults - should NOT get DefaultValueFactory (trivial)
                                                          public string[] EmptyStringArray { get; set; } = [];
                                                          public int[] EmptyIntArray { get; set; } = new int[0];
                                                          public string[] EmptyStringArrayClassic { get; set; } = new string[] { };
                                                          
                                                          // Arrays with meaningful defaults - should get DefaultValueFactory
                                                          public string[] DefaultStringArray { get; set; } = ["default", "values"];
                                                          public int[] DefaultIntArray { get; set; } = [1, 2, 3];
                                                          public string[] SingleValueArray { get; set; } = ["single"];
                                                          
                                                          // Arrays initialized with meaningful values using different syntax
                                                          public string[] ClassicStringArray { get; set; } = new string[] { "classic", "syntax" };
                                                          public int[] ClassicIntArray { get; set; } = new int[] { 42, 99 };
                                                      }
                                                  
                                                      [CommandLineOptionsFor(typeof(AppConfig))]
                                                      public partial class AppConfigOptions { }
                                                  }
                                                  """;
}