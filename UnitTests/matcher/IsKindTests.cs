using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Framework;
using SyntaxSearch;
using NUnit = NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class IsKindTests
    {
        [NUnit::TestCase("invocation()", SyntaxKind.InvocationExpression)]
        [NUnit::TestCase("a?.invocation()", SyntaxKind.ConditionalAccessExpression)]
        [NUnit::TestCase("new int[43]", SyntaxKind.ArrayCreationExpression)]
        [NUnit::TestCase("new object()", SyntaxKind.ObjectCreationExpression)]
        [NUnit::TestCase("a + b", SyntaxKind.AddExpression)]
        [NUnit::TestCase("a * b", SyntaxKind.MultiplyExpression)]
        [NUnit::TestCase("a / (1 + a)", SyntaxKind.DivideExpression)]
        [NUnit::TestCase("ab[13, 432]", SyntaxKind.ElementAccessExpression)]
        public void Match(string code, SyntaxKind kind)
        {
            var node = SyntaxFactory.ParseExpression(code);
            NUnit::Assert.That(Is.Kind(kind).IsMatch(node), NUnit::Is.True, $"expected {node.Kind()} to match {kind}");
        }

    }
}