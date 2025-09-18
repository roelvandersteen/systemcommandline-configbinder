using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SystemCommandLine.ConfigBinder.Generators;

/// <summary>
///     Extracts default value expressions from property initializers and converts them
///     to string representations suitable for code generation.
/// </summary>
internal static class DefaultExpressionExtractor
{
    /// <summary>
    ///     Extracts and parses the default expression from a property symbol.
    /// </summary>
    /// <param name="prop">The property symbol to analyze.</param>
    /// <returns>
    ///     A string representation of the default expression, or null if no expression is found.
    /// </returns>
    public static string? GetDefaultExpression(IPropertySymbol prop)
    {
        SyntaxReference? syntaxReference = prop.DeclaringSyntaxReferences.FirstOrDefault();
        var propertyDeclaration = syntaxReference?.GetSyntax() as PropertyDeclarationSyntax;
        ExpressionSyntax? initializerValue = propertyDeclaration?.Initializer?.Value;

        if (initializerValue == null)
        {
            return null;
        }

        return initializerValue switch
        {
            LiteralExpressionSyntax literal => literal.Token.Text,
            MemberAccessExpressionSyntax memberAccess => GetEnumMemberExpression(memberAccess, prop),
            IdentifierNameSyntax identifier => GetIdentifierExpression(identifier, prop),
            CollectionExpressionSyntax collectionExpression => GetCollectionExpression(collectionExpression),
            ArrayCreationExpressionSyntax arrayCreation => GetArrayCreationExpression(arrayCreation),
            ImplicitArrayCreationExpressionSyntax implicitArrayCreation => GetImplicitArrayCreationExpression(implicitArrayCreation),
            _ => null
        };
    }

    private static string? GetArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreation)
    {
        if (arrayCreation.Initializer == null)
        {
            // Handle cases like "new int[0]"
            var rankSpecifiers = arrayCreation.Type.RankSpecifiers;
            if (rankSpecifiers.Count <= 0)
            {
                return null;
            }

            ArrayRankSpecifierSyntax firstRank = rankSpecifiers[0];
            if (firstRank.Sizes.Count > 0 && firstRank.Sizes[0].ToString() == "0")
            {
                return arrayCreation.ToString();
            }

            return null;
        }

        var elements = arrayCreation.Initializer.Expressions.Select(e => e.ToString()).ToList();
        return elements.Count == 0 ? $"new {arrayCreation.Type} {{ }}" : $"new {arrayCreation.Type} {{ {string.Join(", ", elements)} }}";
    }

    private static string GetCollectionExpression(CollectionExpressionSyntax collectionExpression)
    {
        var elements = collectionExpression.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression.ToString()).ToList();

        return $"[{string.Join(", ", elements)}]";
    }

    private static string? GetEnumMemberExpression(MemberAccessExpressionSyntax memberAccess, IPropertySymbol prop)
    {
        if (prop.Type.TypeKind != TypeKind.Enum)
        {
            return null;
        }

        var enumTypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var memberName = memberAccess.Name.Identifier.ValueText;
        return $"{enumTypeName}.{memberName}";
    }

    private static string? GetIdentifierExpression(IdentifierNameSyntax identifier, IPropertySymbol prop)
    {
        if (prop.Type.TypeKind != TypeKind.Enum)
        {
            return null;
        }

        var enumTypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var memberName = identifier.Identifier.ValueText;
        return $"{enumTypeName}.{memberName}";
    }

    private static string GetImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
    {
        var elements = implicitArrayCreation.Initializer.Expressions.Select(e => e.ToString()).ToList();
        return elements.Count == 0 ? "new[] { }" : $"new[] {{ {string.Join(", ", elements)} }}";
    }
}