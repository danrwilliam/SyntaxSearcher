using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Is]
    public partial class NumericConstantExpressionMatcher : SyntaxSearch.Matchers.Explicit.ExpressionSyntaxMatcher, INodeMatcher
    {
        //private Optional<double> _value;

        //public NumericConstantExpressionMatcher WithValue(double value) => new NumericConstantExpressionMatcher
        //{
        //    _value = value
        //};

        private bool IsNodeMatchNoValue(SyntaxNode node, CaptureStore store)
        {
            if (node is ParenthesizedExpressionSyntax p)
            {
                node = p.Expression;
            }

            if (node.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                return true;
            }
            else if (node is PrefixUnaryExpressionSyntax
            {
                Operand: LiteralExpressionSyntax l
            } && l.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                return true;
            }
            else if (node is BinaryExpressionSyntax binary)
            {
                return IsNodeMatch(binary.Left, store) && IsNodeMatch(binary.Right, store);
            }
            else
            {
                return false;
            }
        }

        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            //if (_value.HasValue)
            //{
                return IsNodeMatch(node, store);
            //}
            //else
            //{
            //    return IsNodeMatchNoValue(node, store);
            //}
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store) => true;
    }
}
