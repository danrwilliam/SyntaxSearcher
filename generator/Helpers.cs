using Microsoft.CodeAnalysis;
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
        public static MatchProperty[] GetNamedProperties(ITypeSymbol parameterType, INamedTypeSymbol syntaxNodeType, INamedTypeSymbol syntaxTokenType)
        {
            return [.. parameterType.GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName).Select(f =>
            {
                if (f.Type.IsSubclassOf(syntaxNodeType))
                    return new { prop = f, isList = false, include = true };

                if (f.Type.IsSubclassOf(syntaxTokenType) && f.Name is "Identifier" or "Keyword" or "Token")
                    return new { prop = f, isList = false, include = true };

                var n = f.Type as INamedTypeSymbol;
                if (n.TypeArguments.Length == 1 && n.TypeArguments[0].IsSubclassOf(syntaxNodeType))
                {
                    return new { prop = f, isList = true, include = true };

                }
                return new { prop = f, isList = false, include = false };
            })
            .Where(f => f.include && f.prop.Name != "Parent")
            .Select(f => new MatchProperty(f.prop, f.isList))];
        }
    }
}