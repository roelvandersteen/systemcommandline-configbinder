using SystemCommandLine.ConfigBinder;

namespace ConfigBinder.CodeGeneration;

[CommandLineOptionsFor(typeof(AppConfig))] public partial class AppConfigOptions;