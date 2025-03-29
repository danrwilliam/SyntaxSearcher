using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Does("Match")]
    public partial class MatchCapture : LogicalMatcher
    {
        [With]
        private string _name;

        [UseConstructor]
        public MatchCapture(string name)
        {
            _name = name;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (!string.IsNullOrWhiteSpace(_name)
                && store.CapturedGroups.TryGetValue(_name, out var capturedNode))
            {
                if (capturedNode is VariableDeclaratorSyntax variableDeclSyntax
                    && node is IdentifierNameSyntax identifier)
                {
                    return SyntaxFactory.AreEquivalent(variableDeclSyntax.Identifier, identifier.Identifier);
                }
                else
                {
                    return SyntaxFactory.AreEquivalent(node, capturedNode);
                }
            }
            return false;
        }
    }
}
