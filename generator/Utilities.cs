using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Text;

namespace SyntaxSearcher.Generators
{
    public static class Utilities
    {
        /// <summary>
        /// Parse given StringBuilder contents as a <see cref="SyntaxTree"/>
        /// and normalizes the whitespace
        /// </summary>
        /// <remarks>
        /// This avoids needing to handle indentation when building up
        /// source generator text
        /// </remarks>
        /// <param name="builder"></param>
        /// <returns>normalized string</returns>
        public static string Normalize(StringBuilder builder)
        {
            return Normalize(builder.ToString());
        }

        /// <summary>
        /// Parses given string as a <see cref="SyntaxTree"/> and 
        /// normalizes the whitespace
        /// </summary>
        /// <remarks>
        /// This avoids needing to handle indentation when building up
        /// source generator text
        /// </remarks>/// 
        /// <param name="text"></param>
        /// <returns>normalized string</returns>
        public static string Normalize(string text)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetRoot().NormalizeWhitespace();
            return root.ToFullString();
        }

        public static bool IsSubclassOf(this ITypeSymbol typeSymbol, ITypeSymbol candidateBase)
        {
            if (typeSymbol is null)
                return false;

            if (SymbolEqualityComparer.Default.Equals(typeSymbol, candidateBase))
                return true;

            return typeSymbol.BaseType.IsSubclassOf(candidateBase);
        }

        public static IEnumerable<ITypeSymbol> BaseTypes(this ITypeSymbol t)
        {
            t = t?.BaseType;
            while (t is not null)
            {
                yield return t;
                t = t.BaseType;
            }
        }

        public static string GetMatcherBase(this ITypeSymbol t)
        {
            if (t.IsAbstract && !t.Name.StartsWith("Base"))
            {
                return $"SyntaxSearch.Matchers.Explicit.{t.Name}Matcher";
            }
            else if (t.Name == nameof(SyntaxToken))
            {
                return "ITokenMatcher";
            }
            else
            {
                return "INodeMatcher";
            }
        }
    }
}
