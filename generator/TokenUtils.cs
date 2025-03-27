using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    public static class TokenUtils
    {
        public static ImmutableDictionary<string, string> Properties = new Dictionary<string, string>()
        {
            {"Identifier", "Text" },
        }.ToImmutableDictionary();
    }
}