using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Matches a member expression that ends with a certain access pattern
    /// </summary>
    /// <remarks>
    /// If constructed with A.B, this would match A.B and C.A.B
    /// </remarks>
    [Is]
    public partial class MemberNameMatcher : LogicalMatcher, INodeMatcher
    {
        [With]
        private LogicalOrNodeMatcher<Matchers.Explicit.IdentifierNameMatcher> _name;

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node switch
            {
                IdentifierNameSyntax or MemberAccessExpressionSyntax when _name is null => true,
                IdentifierNameSyntax name => _name.IsMatch(name, store),
                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } => _name.IsMatch(name, store),
                _ => false
            };
        }
    }
}
