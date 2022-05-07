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
        public static (IPropertySymbol prop, bool isList)[] GetNamedProperties(ITypeSymbol parameterType, INamedTypeSymbol syntaxNodeType)
        {
            return parameterType.GetMembers().OfType<IPropertySymbol>().Select(f =>
            {
                if (f.Type.IsSubclassOf(syntaxNodeType))
                    return new { prop = f, isList = false, include = true };
                var n = f.Type as INamedTypeSymbol;
                if (n.TypeArguments.Length == 1 && n.TypeArguments[0].IsSubclassOf(syntaxNodeType))
                {
                    return new { prop = f, isList = true, include = true };

                }
                return new { prop = f, isList = false, include = false };
            }).Where(f => f.include)
            .Where(f => f.prop.Name != "Parent")
            .Select(f => (f.prop, f.isList)).ToArray();
        }
    }
}