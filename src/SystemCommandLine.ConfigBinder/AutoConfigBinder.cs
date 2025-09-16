using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.CommandLine.Parsing;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SystemCommandLine.ConfigBinder;

/// <summary>
///     Provides automatic, reflection-driven generation of <see cref="Option" /> instances for a configuration
///     <typeparamref name="T" /> and binds parsed command line values back into a strongly typed object.
/// </summary>
/// <typeparam name="T">The configuration POCO type whose public, settable instance properties are converted to options.</typeparam>
/// <remarks>
///     <para>
///         The binder performs a single reflective pass the first time <see cref="AddOptionsTo(Command)" /> is invoked.
///         For each eligible property it creates a <c>--kebab-cased-name</c> option (derived from the property name) and,
///         for boolean properties with a default of <c>true</c>, also creates an inverse flag (e.g. <c>--no-feature-x</c>)
///         allowing explicit disabling. Fast compiled delegates are cached to avoid recurring reflection cost when
///         <see cref="Get(ParseResult)" /> is called.
///     </para>
///     <para>
///         After binding, DataAnnotations validation is executed. Attributes such as <see cref="RequiredAttribute" /> are
///         handled partly by System.CommandLine (presence) while range or custom validators are enforced post-population.
///         Validation failures are aggregated into a single <see cref="ValidationException" /> for concise upstream
///         handling.
///     </para>
///     <list type="bullet">
///         <listheader>
///             <term>Features</term>
///         </listheader>
///         <item>
///             <description>Automatic option name generation (camel/PascalCase → kebab-case with leading <c>--</c>).</description>
///         </item>
///         <item>
///             <description>Inverse boolean option synthesis (<c>--no-*</c>) when a property defaults to <c>true</c>.</description>
///         </item>
///         <item>
///             <description>Default value inference: only non-trivial defaults are assigned to options.</description>
///         </item>
///         <item>
///             <description>DataAnnotations validation (all properties, recursive, with aggregated error messaging).</description>
///         </item>
///         <item>
///             <description>Minimal allocations via cached delegates and single initialization pass.</description>
///         </item>
///     </list>
///     <para>
///         Thread-safety: Instances are intended for single pipeline setup (call <see cref="AddOptionsTo(Command)" /> once
///         during application startup). Subsequent concurrent calls to <see cref="Get(ParseResult)" /> are safe after
///         initialization completes because only cached delegates and immutable metadata are accessed.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp"><![CDATA[
/// var root = new RootCommand();
/// var binder = new AutoConfigBinder<AppConfig>();
/// binder.AddOptionsTo(root);
/// 
/// root.SetAction(parseResult =>
/// {
///     var config = binder.Get(parseResult);
///     Console.WriteLine($"Host: {config.Host} Port: {config.Port}");
///     return 0;
/// });
/// 
/// return rootCommand.Parse(args).Invoke();
/// ]]></code>
/// </example>
public sealed class AutoConfigBinder<T> where T : new()
{
    private readonly List<BoundProperty> _boundProperties = [];
    private T? _defaultInstance;
    private bool _isInitialized;

    /// <summary>
    ///     Reflects over the public, settable instance properties of <typeparamref name="T" /> (one-time initialization)
    ///     and adds a corresponding <see cref="Option" /> for each to the provided <paramref name="command" />.
    ///     For boolean properties that have a default value of <c>true</c>, an additional inverse option
    ///     (<c>--no-property-name</c>) is generated that allows explicitly disabling the flag.
    ///     Subsequent calls are ignored once initialization has completed.
    /// </summary>
    /// <param name="command">The root or sub command to which generated options should be appended.</param>
    /// <remarks>
    ///     This method performs reflection only on the first invocation and caches compiled delegates for fast value
    ///     retrieval &amp; assignment during <see cref="Get(ParseResult)" />.
    /// </remarks>
    public void AddOptionsTo(Command command)
    {
        if (_isInitialized)
        {
            return;
        }

        var parseResultGetValueMethod = ResolveParseResultGetValueDefinition();
        _defaultInstance = new T();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property is not { CanRead: true, CanWrite: true })
            {
                continue;
            }

            var optionBuildResult = OptionFactory.Create(property, _defaultInstance);
            command.Options.Add(optionBuildResult.PrimaryOption);
            if (optionBuildResult.InverseBoolOption is not null)
            {
                command.Options.Add(optionBuildResult.InverseBoolOption);
            }

            _boundProperties.Add(new BoundProperty
            {
                Property = property,
                InverseBoolOption = optionBuildResult.InverseBoolOption,
                BoolInverse = optionBuildResult.BoolInverse,
                GetValueDelegate = CompileParseResultAccessor(parseResultGetValueMethod, property, optionBuildResult.PrimaryOption),
                AssignDelegate = BuildAssignmentDelegate(property)
            });
        }

        _isInitialized = true;
    }

    /// <summary>
    ///     Produces a new <typeparamref name="T" /> instance populated from the supplied <paramref name="parseResult" />.
    ///     Values are pulled from the generated options created during <see cref="AddOptionsTo(Command)" />; inverse
    ///     boolean options (e.g. <c>--no-feature-x</c>) override their corresponding positive boolean if present.
    ///     After population, DataAnnotations validation is executed; if validation fails a <see cref="ValidationException" />
    ///     is thrown with aggregated error messages.
    /// </summary>
    /// <param name="parseResult">The parse result obtained from invoking the parser on the command line arguments.</param>
    /// <returns>A fully populated configuration object of type <typeparamref name="T" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if called before <see cref="AddOptionsTo(Command)" />.</exception>
    /// <exception cref="ValidationException">Thrown when DataAnnotations validation fails for the constructed object.</exception>
    public T Get(ParseResult parseResult)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("AddOptionsTo must be called before Get.");
        }

        var configuration = new T();
        foreach (var boundProperty in _boundProperties)
        {
            var value = boundProperty.GetValueDelegate(parseResult);

            if (boundProperty is { BoolInverse: true, InverseBoolOption: not null })
            {
                var inversePresent = parseResult.RootCommandResult
                    .Children
                    .OfType<OptionResult>()
                    .Any(r => r.Option == boundProperty.InverseBoolOption);
                if (inversePresent)
                {
                    value = false;
                }
            }

            if (value is null && boundProperty.Property.PropertyType.IsValueType &&
                Nullable.GetUnderlyingType(boundProperty.Property.PropertyType) is null)
            {
                continue;
            }

            boundProperty.AssignDelegate(configuration, value);
        }

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(configuration);
        if (Validator.TryValidateObject(configuration, context, validationResults, true))
        {
            return configuration;
        }

        var message = new StringBuilder("Validation failed:\n");
        foreach (var vr in validationResults)
        {
            message.Append(" - ").Append(vr.ErrorMessage).Append('\n');
        }

        throw new ValidationException(message.ToString());
    }

    private static Action<T, object?> BuildAssignmentDelegate(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(T), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(Expression.Property(target, property), Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<T, object?>>(assign, target, value).Compile();
    }

    private static Func<ParseResult, object?> CompileParseResultAccessor(MethodInfo openGetValueMethod, PropertyInfo property,
        Option option)
    {
        var closedGetValueMethod = openGetValueMethod.MakeGenericMethod(property.PropertyType);
        var prParam = Expression.Parameter(typeof(ParseResult), "result");
        var argumentType = typeof(Option<>).MakeGenericType(property.PropertyType);
        var call = Expression.Call(prParam, closedGetValueMethod, Expression.Convert(Expression.Constant(option), argumentType));
        var boxed = Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<ParseResult, object?>>(boxed, prParam).Compile();
    }

    private static Delegate GenerateDefaultValueLambda(PropertyInfo property, object defaultValue)
    {
        var funcType = typeof(Func<,>).MakeGenericType(typeof(ArgumentResult), property.PropertyType);
        var param = Expression.Parameter(typeof(ArgumentResult), "_");
        var constant = Expression.Constant(defaultValue, property.PropertyType);
        return Expression.Lambda(funcType, constant, param).Compile();
    }

    internal static string GetOptionName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var sb = new StringBuilder("--");
        for (var i = 0; i < propertyName.Length; i++)
        {
            if (i > 0 && char.IsUpper(propertyName[i]) && char.IsLower(propertyName[i - 1]))
            {
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(propertyName[i]));
        }

        return sb.ToString();
    }

    internal static bool IsDefaultStructValue(object value, Type type)
    {
        return type.IsValueType && Equals(value, Activator.CreateInstance(type));
    }

    internal static bool IsTrivialReferenceDefault(object value)
    {
        return value switch { string s => string.IsNullOrEmpty(s), Array a => a.Length == 0, _ => false };
    }

    private static MethodInfo ResolveParseResultGetValueDefinition()
    {
        return typeof(ParseResult).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(m => m is { Name: nameof(ParseResult.GetValue), IsGenericMethod: true } &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType.IsGenericType &&
                        m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Option<>));
    }

    private sealed class BoundProperty
    {
        public PropertyInfo Property { get; set; } = null!;
        public Option? InverseBoolOption { get; set; }
        public bool BoolInverse { get; set; }
        public Func<ParseResult, object?> GetValueDelegate { get; set; } = null!;
        public Action<T, object?> AssignDelegate { get; set; } = null!;
    }

    internal struct OptionBuildResult
    {
        public OptionBuildResult(Option primary, Option? inverse, bool flag)
        {
            PrimaryOption = primary;
            InverseBoolOption = inverse;
            BoolInverse = flag;
        }

        public Option PrimaryOption;
        public Option? InverseBoolOption;
        public bool BoolInverse;
    }

    internal static class OptionFactory
    {
        public static OptionBuildResult Create(PropertyInfo property, T defaultInstance)
        {
            var propertyType = property.PropertyType;
            var option = Instantiate(property, propertyType);
            ApplyDescriptionAndRequired(property, option);
            var defaultValue = property.GetValue(defaultInstance);
            TryApplyDefault(property, propertyType, option, defaultValue);
            var (inverse, flag) = MaybeCreateInverseBool(property, propertyType, defaultValue);
            return new OptionBuildResult(option, inverse, flag);
        }

        private static Option Instantiate(PropertyInfo property, Type propertyType)
        {
            var optionType = typeof(Option<>).MakeGenericType(propertyType);
            var optionName = GetOptionName(property.Name);
            return (Option)Activator.CreateInstance(optionType, optionName)!;
        }

        private static void ApplyDescriptionAndRequired(PropertyInfo property, Option option)
        {
            option.Description = property.GetCustomAttribute<DisplayAttribute>()?.Description ?? $"Sets the {property.Name} value";
            option.Required = property.GetCustomAttribute<RequiredAttribute>() != null;
        }

        private static void TryApplyDefault(PropertyInfo property, Type propertyType, Option option, object? defaultValue)
        {
            if (defaultValue is null)
            {
                return;
            }

            if (option.Required)
            {
                return;
            }

            if (IsDefaultStructValue(defaultValue, propertyType))
            {
                return;
            }

            if (IsTrivialReferenceDefault(defaultValue))
            {
                return;
            }

            var optionType = option.GetType();
            var defaultValueFactoryProp = optionType.GetProperty(nameof(Option<T>.DefaultValueFactory));
            if (defaultValueFactoryProp == null)
            {
                return;
            }

            var factory = GenerateDefaultValueLambda(property, defaultValue);
            defaultValueFactoryProp.SetValue(option, factory);
        }

        private static (Option? inverseOption, bool inverseFlag) MaybeCreateInverseBool(PropertyInfo property, Type propertyType,
            object? defaultValue)
        {
            if (propertyType != typeof(bool) || defaultValue is not true)
            {
                return (null, false);
            }

            var inverseName = GetOptionName($"No{property.Name}");
            var inverse = new Option<bool>(inverseName)
            {
                Description = $"Sets {GetOptionName(property.Name)} to false",
                Required = false
            };
            return (inverse, true);
        }
    }
}