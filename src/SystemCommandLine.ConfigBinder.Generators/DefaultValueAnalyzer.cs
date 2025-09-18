using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SystemCommandLine.ConfigBinder.Generators;

/// <summary>
///     Analyzes property default expressions to determine if they represent trivial default values
///     or meaningful custom defaults that should be applied to command-line options.
/// </summary>
internal static class DefaultValueAnalyzer
{
    /// <summary>
    ///     Determines whether a default expression represents a meaningful default that should
    ///     be applied to a command-line option, as opposed to a trivial default value.
    /// </summary>
    /// <param name="prop">The property symbol to analyze.</param>
    /// <param name="defaultExpression">The string representation of the default expression.</param>
    /// <returns>
    ///     <c>true</c> if the default should be applied (it's meaningful);
    ///     <c>false</c> if it's a trivial default that should be omitted.
    /// </returns>
    public static bool ShouldApplyDefault(IPropertySymbol prop, string? defaultExpression)
    {
        if (IsRequired(prop) || defaultExpression is null or "null")
        {
            return false;
        }

        // For enums, always apply non-null defaults (they're not "trivial")
        if (prop.Type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        if (IsNullableGenericType(prop, out INamedTypeSymbol namedType))
        {
            ITypeSymbol underlyingType = namedType.TypeArguments[0];
            return !IsDefaultValueForType(underlyingType, defaultExpression);
        }

        if (prop.Type.IsValueType)
        {
            return !IsDefaultValueExpression(prop, defaultExpression);
        }

        if (prop.Type.SpecialType == SpecialType.System_String)
        {
            return !defaultExpression.Equals("\"\"") && !defaultExpression.Equals("string.Empty");
        }

        return true;
    }

    private static bool CheckGenericDefault(ITypeSymbol type, string defaultExpression)
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return defaultExpression == $"default({typeName})" || defaultExpression == "default";
    }

    private static bool IsDefaultEnumValue(ITypeSymbol enumType, string defaultExpression)
    {
        var enumTypeName = enumType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Checks for explicit default patterns in enum types.
        // This method currently assumes that explicit enum values, such as "default", "None", or "Default",
        // are meaningful and should be treated as non-trivial. A more complex analysis would be required
        // to handle specific enum member references accurately.
        return defaultExpression == $"default({enumTypeName})" || defaultExpression == "default" || defaultExpression.EndsWith(".None") ||
               defaultExpression.EndsWith(".Default");
    }

    private static bool IsDefaultValueExpression(IPropertySymbol prop, string defaultExpression)
    {
        if (prop.Type.IsValueType)
        {
            return IsDefaultValueForType(prop.Type, defaultExpression);
        }

        if (prop.Type.SpecialType == SpecialType.System_String)
        {
            return defaultExpression is "\"\"" or "string.Empty";
        }

        return defaultExpression == $"default({prop.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})";
    }

    private static bool IsDefaultValueForType(ITypeSymbol type, string defaultExpression)
    {
        try
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => TryParseAndCheckDefault(defaultExpression, bool.TryParse, false),
                SpecialType.System_Byte => TryParseAndCheckDefault<byte>(defaultExpression, byte.TryParse, 0),
                SpecialType.System_SByte => TryParseAndCheckDefault<sbyte>(defaultExpression, sbyte.TryParse, 0),
                SpecialType.System_Int16 => TryParseAndCheckDefault<short>(defaultExpression, short.TryParse, 0),
                SpecialType.System_UInt16 => TryParseAndCheckDefault<ushort>(defaultExpression, ushort.TryParse, 0),
                SpecialType.System_Int32 => TryParseAndCheckDefault(defaultExpression, int.TryParse, 0),
                SpecialType.System_UInt32 => TryParseAndCheckDefault<uint>(defaultExpression, uint.TryParse, 0),
                SpecialType.System_Int64 => TryParseAndCheckDefault<long>(defaultExpression, long.TryParse, 0),
                SpecialType.System_UInt64 => TryParseAndCheckDefault<ulong>(defaultExpression, ulong.TryParse, 0),
                SpecialType.System_Single => TryParseAndCheckDefault(defaultExpression, float.TryParse, 0.0f),
                SpecialType.System_Double => TryParseAndCheckDefault(defaultExpression, double.TryParse, 0.0),
                SpecialType.System_Decimal => TryParseAndCheckDefault(defaultExpression, decimal.TryParse, 0m),
                SpecialType.System_Char => TryParseCharDefault(defaultExpression),
                SpecialType.System_DateTime => TryParseDateTimeDefault(defaultExpression),
                _ when type.TypeKind == TypeKind.Enum => IsDefaultEnumValue(type, defaultExpression),
                _ => CheckGenericDefault(type, defaultExpression)
            };
        }
        catch
        {
            // If parsing fails, assume it's not a default value
            return false;
        }
    }

    private static bool IsNullableGenericType(IPropertySymbol prop, out INamedTypeSymbol namedTypeSymbol)
    {
        if (prop.Type is INamedTypeSymbol { IsGenericType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            namedTypeSymbol = namedType;
            return true;
        }

        namedTypeSymbol = null!;
        return false;
    }

    private static bool IsRequired(IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name is "Required" or "RequiredAttribute");
    }

    private static bool TryParseAndCheckDefault<T>(string expression, TryParseDelegate<T> tryParse, T defaultValue) where T : IEquatable<T>
    {
        // Remove common suffixes that don't affect the value
        var cleanExpression = expression.TrimEnd('f', 'F', 'd', 'D', 'm', 'M', 'L', 'U');

        return tryParse(cleanExpression, out T? result) && result.Equals(defaultValue);
    }

    private static bool TryParseCharDefault(string defaultExpression)
    {
        if (!defaultExpression.StartsWith("'") || !defaultExpression.EndsWith("'") || defaultExpression.Length < 3)
        {
            return false;
        }

        if (defaultExpression == "'\\0'")
        {
            return true;
        }

        // For other char literals, extract the character
        var charContent = defaultExpression.Substring(1, defaultExpression.Length - 2);
        return charContent.Length switch
        {
            1 => charContent[0] == '\0',
            2 when charContent[0] == '\\' => charContent[1] switch
            {
                '0' => true,
                _ => false
            },
            _ => false
        };
    }

    private static bool TryParseDateTimeDefault(string defaultExpression)
    {
        return defaultExpression == "DateTime.MinValue" || defaultExpression == "default(DateTime)" ||
               defaultExpression == "default(System.DateTime)";
    }

    private delegate bool TryParseDelegate<T>(string input, out T result);
}