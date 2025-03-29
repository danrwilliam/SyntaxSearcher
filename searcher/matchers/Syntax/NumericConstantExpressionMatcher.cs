using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{
    [Is]
    public partial class NumericConstantExpressionMatcher : SyntaxSearch.Matchers.ExpressionSyntaxMatcher, INodeMatcher
    {
        //private Optional<double> _value;

        //public NumericConstantExpressionMatcher WithValue(double value) => new NumericConstantExpressionMatcher
        //{
        //    _value = value
        //};

        private static readonly ImmutableHashSet<SyntaxKind> NumericKinds =
        [
            SyntaxKind.FloatKeyword,
            SyntaxKind.DoubleKeyword,
            SyntaxKind.LongKeyword,
            SyntaxKind.ULongKeyword,
            SyntaxKind.IntKeyword,
            SyntaxKind.UIntKeyword,
            SyntaxKind.ShortKeyword,
            SyntaxKind.UShortKeyword
        ];

        private bool IsNodeMatchNoValue(SyntaxNode node, CaptureStore store)
        {
            if (node is ParenthesizedExpressionSyntax p)
            {
                node = p.Expression;
            }
            if (node is CastExpressionSyntax
                {
                    Type: PredefinedTypeSyntax { Keyword: { } predefined }
                } castExpr && NumericKinds.Contains(predefined.Kind()))
            {
                node = castExpr.Expression;
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
                return IsNodeMatchNoValue(binary.Left, store) && IsNodeMatchNoValue(binary.Right, store);
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
            return IsNodeMatchNoValue(node, store);
            //}
            //else
            //{
            //    return IsNodeMatchNoValue(node, store);
            //}
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store) => true;
    }
}
