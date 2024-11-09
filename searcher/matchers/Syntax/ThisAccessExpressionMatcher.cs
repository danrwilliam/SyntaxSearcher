using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Is]
    public partial class ThisAccessExpressionMatcher : Matchers.Explicit.ExpressionSyntaxMatcher, INodeMatcher
    {
        public ThisAccessExpressionMatcher(string captureName, string matchName) : base(captureName, matchName)
        {
        }

        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            if (node is MemberAccessExpressionSyntax member)
            {
                var left = member.Expression;
                while (left is MemberAccessExpressionSyntax m)
                {
                    left = m;
                }

                return left is ThisExpressionSyntax;
            }
            else
            {
                return false;
            }
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store) => true;
    }
}
