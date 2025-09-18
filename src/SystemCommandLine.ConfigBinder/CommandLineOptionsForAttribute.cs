using System.CommandLine;

namespace SystemCommandLine.ConfigBinder;

/// <summary>
///     Identifies a <c>partial</c> class as the generated command-line options container for a configuration (POCO) type.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to an (empty) <c>partial</c> class and supply the configuration type whose public settable
///         properties you want to expose as <see cref="Option{T}" /> instances. A C# source generator (shipped in this
///         package) inspects the referenced configuration type at compile time and emits a companion <c>partial</c>
///         containing:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>One <see cref="Option{T}" /> static property per eligible configuration property.</description>
///         </item>
///         <item>
///             <description>An <c>AddTo(Command)</c> helper that adds all generated options to a <see cref="Command" />.</description>
///         </item>
///         <item>
///             <description>A <c>GetFrom(ParseResult)</c> helper that materializes and populates a new config instance.</description>
///         </item>
///     </list>
///     <para>
///         By convention option names are derived from property names (e.g. <c>ServiceEndpoint</c> becomes
///         <c>--service-endpoint</c>).
///     </para>
///     <para>
///         The generation step is purely compile-time; no reflection is used at runtime. If you rename or modify the
///         configuration properties, the generated code updates on the next build.
///     </para>
///     <para>
///         <b>Eligibility rules (current):</b> public instance, settable properties with a supported type (primitive,
///         enum, string, etc.).
///     </para>
///     <para>
///         <b>Thread-safety:</b> Generated <see cref="Option{T}" /> instances are static and intended to be reused
///         across parses.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp"><![CDATA[
/// public class AppConfig
/// {
///     public string Endpoint { get; set; } = "https://localhost";
///     public bool Diagnostics { get; set; } = true;
///     public int Retries { get; set; } = 3;
/// }
/// 
/// [CommandLineOptionsFor(typeof(AppConfig))]
/// public partial class AppConfigOptions { }
/// 
/// // Usage inside Program.cs
/// var root = new RootCommand("Sample tool");
/// AppConfigOptions.AddTo(root);
/// 
/// root.SetAction(async (parseResult, cancellationToken) =>
/// {
///     AppConfig config = AppConfigOptions.GetFrom(parseResult);
///     await Console.Out.WriteLineAsync(new StringBuilder("Endpoint: ").Append(config.Endpoint), cancellationToken);
///     return 0;
/// });
/// await root.Parse(args).InvokeAsync(cancellationToken);
/// ]]></code>
/// </example>
/// <param name="configType">The configuration (POCO) <see cref="Type" /> to project into command-line options.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="configType" /> is <c>null</c>.</exception>
/// <seealso cref="Command" />
/// <seealso cref="Option" />
/// <seealso cref="ParseResult" />
[AttributeUsage(AttributeTargets.Class)]
public class CommandLineOptionsForAttribute(Type configType) : Attribute
{
    /// <summary>
    ///     Gets the configuration class <see cref="Type" /> associated with the generated command-line options.
    /// </summary>
    /// <remarks>
    ///     The generator reads this type's public settable properties to build the options and mapping helpers.
    /// </remarks>
    public Type ConfigType { get; } = configType ?? throw new ArgumentNullException(nameof(configType));
}