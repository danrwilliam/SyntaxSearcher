using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers.Explicit;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{
    [Is]
    public partial class BaseAccessExpressionMatcher : Matchers.Explicit.ExpressionSyntaxMatcher, INodeMatcher
    {
        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            if (node is MemberAccessExpressionSyntax member)
            {
                var left = member.Expression;
                while (left is MemberAccessExpressionSyntax m)
                {
                    left = m.Expression;
                }

                return left is BaseExpressionSyntax;
            }
            else
            {
                return false;
            }
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store) => true;
    }
}
