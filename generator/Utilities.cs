using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
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
    }
}
