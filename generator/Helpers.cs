using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearcher.Generators
{
    internal static class Helpers
    {
        /// <summary>
        /// Returns all public members that derive from <see cref="SyntaxNode"/> or is a
        /// list of <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="parameterType"></param>
        /// <param name="syntaxNodeType"></param>
        /// <returns>tuple of the property symbol and if it's a list</returns>
        public static MatchProperty[] GetNamedProperties(IFieldSymbol kind,
                                                         ITypeSymbol parameterType,
                                                         INamedTypeSymbol syntaxNodeType,
                                                         INamedTypeSymbol syntaxTokenType)
        {
            bool includeToken = kind switch
            {
                { HasConstantValue: true, ConstantValue: ushort v } => !NoTokens.Contains((SyntaxKind)v),
                _ => true
            };

            return [.. parameterType.GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName).Select(f =>
            {
                if (f.Type.IsSubclassOf(syntaxNodeType))
                {
                    return new { prop = f, isList = PropertyKind.Normal, include = true };
                }
                if (f.Name == "Modifiers")
                {
                    return new { prop = f, isList = PropertyKind.TokenList, include = true };
                }

                if (includeToken && f.Type.IsSubclassOf(syntaxTokenType) && f.Name is "Identifier" or "Keyword" or "Token")
                    return new { prop = f, isList = PropertyKind.Normal, include = true };
                else if (!includeToken && f.Type.IsSubclassOf(syntaxTokenType) && f.Name is "Identifier" or "Keyword")
                    return new { prop = f, isList = PropertyKind.Normal, include = true };

                var n = f.Type as INamedTypeSymbol;
                if (n.TypeArguments.Length == 1 && n.TypeArguments[0].IsSubclassOf(syntaxNodeType))
                {
                    return new { prop = f, isList = PropertyKind.GenericTokenList, include = true };

                }
                return new { prop = f, isList = PropertyKind.Normal, include = false };
            })
            .Where(f => f.include && f.prop.Name != "Parent")
            .Select(f => new MatchProperty(f.prop, f.isList))];
        }

        /// <summary>
        /// SyntaxKinds that should not expose Token methods
        /// </summary>
        static readonly HashSet<SyntaxKind> NoTokens =
        [
            SyntaxKind.NullLiteralExpression,
            SyntaxKind.ArgListExpression,
            SyntaxKind.TrueLiteralExpression,
            SyntaxKind.FalseLiteralExpression,
            SyntaxKind.DefaultLiteralExpression
        ];
    }
}