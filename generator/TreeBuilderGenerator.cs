using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class TreeBuilderGenerator : ISourceGenerator
    {
        public static ImmutableDictionary<string, string> TokenProperties = new Dictionary<string, string>()
        {
            {"Identifier", "Text" },
        }.ToImmutableDictionary();

        private static readonly ImmutableHashSet<string> _supportAutoCompare = ImmutableArray.Create(
            nameof(IdentifierNameSyntax),
            nameof(MemberAccessExpressionSyntax),
            nameof(GenericNameSyntax)
        ).ToImmutableHashSet();

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
        }
    }
}