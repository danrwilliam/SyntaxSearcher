using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Maybe("Parentheses")]
    public partial class MaybeParenthesesMatcher : Matchers.ExpressionSyntaxMatcher, INodeMatcher
    {
        [With]
        private LogicalOrNodeMatcher<Matchers.ExpressionSyntaxMatcher> _expression;

        public MaybeParenthesesMatcher Default => new MaybeParenthesesMatcher();

        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            return node is ParenthesizedExpressionSyntax or ExpressionSyntax;
        }

        private static SyntaxNode Unwrap(SyntaxNode node)
        {
            while (node is ParenthesizedExpressionSyntax p)
            {
                node = p.Expression;
            }

            return node;
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store)
        {
            return node is null ? false : _expression?.IsMatch(Unwrap(node), store) == true;
        }
    }
}
